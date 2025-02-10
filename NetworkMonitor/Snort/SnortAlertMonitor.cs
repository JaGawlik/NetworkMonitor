using NetworkMonitor.Model;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Net.Http.Json;
using System.Net;

internal class SnortAlertMonitor
{
    private string _snortLogPath;
    private string _apiUrl;
    private string _localIP;
    private Regex _regex;

    public event Action<Alert> AlertReceived;

    private List<Alert> _alertBuffer = new List<Alert>();
    private const int BufferSize = 50; 

    public SnortAlertMonitor(Dispatcher dispatcher)
    {
        _snortLogPath = NetworkMonitor.AppConfiguration.ConfigurationManager.GetSetting("LogFilePath");
        _apiUrl = NetworkMonitor.AppConfiguration.ConfigurationManager.GetSetting("ApiAddress");
        _localIP = NetworkMonitor.AppConfiguration.ConfigurationManager.GetLocalIpAddress();
        if (!File.Exists(_snortLogPath))
        {
            Console.WriteLine($"Plik logów Snorta nie istnieje: {_snortLogPath}");
            return;
        }

        _regex = new Regex(
            @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[(?<sid>\d+:\d+(:\d+)?)\]\s(?<message>.*?)\s\[\*\*\]" +
            @"(?:\s\[Classification:\s(?<classification>.*?)\])?\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>[A-Z0-9-]+)\}\s" +
            // Sekcja źródłowa (srcip i srcport)
            @"(?<srcip>" +
                @"(?:\[(?<srcipv6>[0-9a-fA-F:]+)\]" +       // IPv6 w nawiasach (np. [::1])
                @"|(?<srcipv4>(?:\d{1,3}\.){3}\d{1,3})" +   // IPv4 (np. 192.168.1.1)
                @"|(?<srcipv6_loose>[0-9a-fA-F:]+))" +       // IPv6 bez nawiasów (np. ::1)
            @")" +
            @"(?::(?<srcport>\d+))?" +                       // Port źródłowy (opcjonalny)
            @"\s->\s" +
            // Sekcja docelowa (dstip i dstport)
            @"(?<dstip>" +
                @"(?:\[(?<dstipv6>[0-9a-fA-F:]+)\]" +       // IPv6 w nawiasach
                @"|(?<dstipv4>(?:\d{1,3}\.){3}\d{1,3})" +   // IPv4
                @"|(?<dstipv6_loose>[0-9a-fA-F:]+))" +       // IPv6 bez nawiasów
            @")" +
            @"(?::(?<dstport>\d+))?",                        // Port docelowy (opcjonalny)
            RegexOptions.Compiled
        );
        //_regex = new Regex(
        //    @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[(?<sid>\d+:\d+(:\d+)?)\]\s(?<message>.*?)\s\[\*\*\]\s*(?:\[\*\*\]\s*)*" +
        //    @"(?:\s\[Classification:\s(?<classification>.*?)\])?\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>[A-Z0-9-]+)\}\s" +
        //    // Sekcja źródłowa (srcip i srcport)
        //    @"(?<srcip>" +
        //        @"(?:\[(?<srcipv6>[0-9a-fA-F:]+)\]" +       // IPv6 w nawiasach (np. [::1])
        //        @"|(?<srcipv4>(?:\d{1,3}\.){3}\d{1,3})" +   // IPv4 (np. 192.168.1.1)
        //        @"|(?<srcipv6_loose>[0-9a-fA-F:]+))" +       // IPv6 bez nawiasów (np. ::1)
        //    @")" +
        //    @"(?::(?<srcport>\d+))?" +                       // Port źródłowy (opcjonalny)
        //    @"\s->\s" +
        //    // Sekcja docelowa (dstip i dstport)
        //    @"(?<dstip>" +
        //        @"(?:\[(?<dstipv6>[0-9a-fA-F:]+)\]" +       // IPv6 w nawiasach
        //        @"|(?<dstipv4>(?:\d{1,3}\.){3}\d{1,3})" +   // IPv4
        //        @"|(?<dstipv6_loose>[0-9a-fA-F:]+))" +       // IPv6 bez nawiasów
        //    @")" +
        //    @"(?::(?<dstport>\d+))?",                        // Port docelowy (opcjonalny)
        //    RegexOptions.Compiled
        //);
    }


    public async Task StartMonitoringAsync()
    {
        if (!File.Exists(_snortLogPath))
        {
            Console.WriteLine($"Plik logów Snorta nie istnieje: {_snortLogPath}");
            return;
        }

        using (FileStream fs = new FileStream(_snortLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader sr = new StreamReader(fs))
        {
            fs.Seek(0, SeekOrigin.End);
            Console.WriteLine($"Pozycja wskaźnika w pliku: {fs.Position} (rozmiar pliku: {fs.Length})");

            while (true)
            {
                string line = await sr.ReadLineAsync();
                if (line != null)
                {
                    Console.WriteLine($"Przeczytano linię: {line}");
                    await ProcessLineAsync(line);
                }

                await FetchAlertsFromApiAsync(_localIP);

                // Wyślij pozostałe alerty z bufora
                if (_alertBuffer.Count > 0)
                {
                    await SendAlertsToApiAsync(_alertBuffer);
                    _alertBuffer.Clear();
                }

                await Task.Delay(3000);
            }
        }
    }

    private async Task ProcessLineAsync(string line)
    {
        var match = _regex.Match(line);
        if (match.Success)
        {
            string dateStr = match.Groups["date"].Value;
            string alertMessage = match.Groups["message"].Value;
            string priority = match.Groups["priority"].Value;
            string protocol = match.Groups["protocol"].Value;

            string srcIp = match.Groups["srcip"].Value.Trim('[', ']'); // Usuń nawiasy dla IPv6
            int? srcPort = match.Groups["srcport"].Success ? int.Parse(match.Groups["srcport"].Value) : null;

            // Jeśli regex nie wychwycił portu, użyj metody ExtractIpAndPort
            if (srcPort == null)
            {
                (srcIp, srcPort) = ExtractIpAndPort(match.Groups["srcip"].Value);
            }

            // Analogicznie dla dstip i dstport
            string dstIp = match.Groups["dstip"].Value.Trim('[', ']');
            int? dstPort = match.Groups["dstport"].Success ? int.Parse(match.Groups["dstport"].Value) : null;
            if (dstPort == null)
            {
                (dstIp, dstPort) = ExtractIpAndPort(match.Groups["dstip"].Value);
            }

            string sidString = match.Groups["sid"].Value.Split(':')[1];
            if (!int.TryParse(sidString, out int sid))
            {
                sid = 0;
            }

            if (!ShouldProcessAlert(sid.ToString(), srcIp, 10))
            {
                Console.WriteLine($"Pominięto duplikat alertu SID={sid} od {srcIp}");
                return;
            }

            int currentYear = DateTime.Now.Year;

            var parts = dateStr.Split('-');
            string md = parts[0];
            string hms = parts[1];

            var mdParts = md.Split('/');
            string month = mdParts[0];
            string day = mdParts[1];

            if (DateTime.TryParseExact($"{currentYear}/{month}/{day} {hms}",
                "yyyy/MM/dd HH:mm:ss.ffffff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime timestamp))
            {
                var alert = new Alert
                {
                    Timestamp = timestamp,
                    AlertMessage = alertMessage,
                    SourceIp = srcIp,
                    SourcePort = srcPort,
                    DestinationIp = dstIp,
                    DestinationPort = dstPort,
                    Protocol = protocol,
                    SignatureId = sid,
                    Status = "new",
                    SnortInstance = Dns.GetHostName()
                };

                _alertBuffer.Add(alert);

                if (_alertBuffer.Count >= BufferSize)
                {
                    await SendAlertsToApiAsync(_alertBuffer);
                    _alertBuffer.Clear();
                }

                AlertReceived?.Invoke(alert);
            }
        }
    }

    private (string ip, int? port) ExtractIpAndPort(string ipWithPort)
    {
        if (string.IsNullOrWhiteSpace(ipWithPort))
            return (null, null);

        // Obsługa IPv6 w nawiasach (np. [fe80::1]:443)
        if (ipWithPort.StartsWith("[") && ipWithPort.Contains("]"))
        {
            var match = Regex.Match(ipWithPort, @"^\[(?<ip>[0-9a-fA-F:]+)\](:(?<port>\d+))?$");
            if (match.Success)
            {
                return (match.Groups["ip"].Value, match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value) : null);
            }
        }
        // Obsługa IPv4 (np. 192.168.1.1:80)
        else if (ipWithPort.Contains(".") && ipWithPort.Contains(":"))
        {
            var parts = ipWithPort.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port))
            {
                return (parts[0], port);
            }
        }
        // Obsługa IPv6 bez nawiasów (np. fe80::1:443)
        else if (ipWithPort.Contains(":"))
        {
            var lastColonIndex = ipWithPort.LastIndexOf(":");
            if (lastColonIndex > 0 && int.TryParse(ipWithPort.Substring(lastColonIndex + 1), out int port))
            {
                return (ipWithPort.Substring(0, lastColonIndex), port);
            }
        }

        // Jeśli nie ma portu
        return (ipWithPort, null);
    }

    private async Task SendAlertsToApiAsync(List<Alert> alerts)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{_apiUrl}/api/alerts/batch", alerts);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Wysłano {alerts.Count} alertów do API.");
            }
            else
            {
                Console.WriteLine($"Błąd podczas wysyłania alertów do API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas komunikacji z API: {ex.Message}");
        }
    }
      


    private async Task FetchAlertsFromApiAsync(string localIp)
    {
        using var httpClient = new HttpClient();
        try
        {
            string requestUrl = $"{_apiUrl}/api/alerts?ip={localIp}";
            Console.WriteLine($"Próbuję pobrać alerty z: {requestUrl}");

            var response = await httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var alerts = await response.Content.ReadFromJsonAsync<List<Alert>>();
                if (alerts != null && alerts.Any())
                {
                    foreach (var alert in alerts)
                    {
                        AlertReceived?.Invoke(alert);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Serwer API zwrócił błąd: {response.StatusCode} {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas komunikacji z API: {ex.Message}");
        }
    }

    public async Task<List<(int Sid, string Message, int Count)>> GetFrequentAlertsAsync(int topN = 10)
    {
        using var httpClient = new HttpClient();
        try
        {
            var requestUrl = $"{_apiUrl}/api/alerts";
            var allAlerts = await httpClient.GetFromJsonAsync<List<Alert>>(requestUrl);

            if (allAlerts == null || !allAlerts.Any())
            {
                Console.WriteLine("Nie pobrano żadnych alertów z API.");
                return new List<(int Sid, string Message, int Count)>();
            }

            var groupedAlerts = allAlerts
                .GroupBy(alert => new { alert.SignatureId, alert.AlertMessage }) // Grupowanie po `Sid` i `AlertMessage`
                .Select(group => new
                {
                    Sid = group.Key.SignatureId,
                    Message = group.Key.AlertMessage,
                    Count = group.Count()
                })
                .OrderByDescending(group => group.Count)
                .Take(topN)
                .Select(group => (group.Sid, group.Message, group.Count))
                .ToList();

            Console.WriteLine($"Liczba grupowanych alertów: {groupedAlerts.Count}");
            foreach (var alert in groupedAlerts)
            {
                Console.WriteLine($"SID: {alert.Sid}, Wiadomość: {alert.Message}, Liczba wystąpień: {alert.Count}");
            }

            return groupedAlerts;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Błąd komunikacji z API: {ex.Message}");
            return new List<(int Sid, string Message, int Count)>();
        }
    }

    private HashSet<string> _processedAlerts = new HashSet<string>();

    //Pomijanie przetworzonych alertów
    private bool ShouldProcessAlert(string sid, string srcIp, int seconds)
    {
        string key = $"{sid}:{srcIp}";

        if (_processedAlerts.Contains(key))
        {
            return false; // Ignoruj alert
        }

        _processedAlerts.Add(key); 
        return true; 
    }

}
