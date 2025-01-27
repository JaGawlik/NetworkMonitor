using NetworkMonitor.Model;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Net.Http.Json;

internal class SnortAlertMonitor
{
    private string _snortLogPath;
    private string _apiUrl;
    private string _localIP;
    private Regex _regex;

    public event Action<Alert> AlertReceived;

    private Dictionary<string, DateTime> _recentAlerts = new();

    public SnortAlertMonitor(string logPath, string apiUrl, Dispatcher dispatcher)
    {
        _snortLogPath = logPath;
        _apiUrl = apiUrl;
        _localIP = NetworkMonitor.AppConfiguration.ConfigurationManager.GetLocalIpAddress();

        if (!File.Exists(_snortLogPath))
        {
            Console.WriteLine($"Plik logów Snorta nie istnieje: {_snortLogPath}");
            return;
        }

        _regex = new Regex(
            @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[\d+:\d+:\d+\]\s(?<message>.*?)\s\[\*\*\](\s\[Classification:\s.*?\])?\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>\w+)\}\s(?<srcip>[a-fA-F0-9\:\.]+):\d+\s->\s(?<dstip>[a-fA-F0-9\:\.]+):\d+",
            RegexOptions.Compiled
        );

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
            string srcIp = match.Groups["srcip"].Value;
            string dstIp = match.Groups["dstip"].Value;

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
                    DestinationIp = dstIp,
                    Protocol = protocol,
                    Status = "new",
                    SnortInstance = "Snort_PC_01"
                };

                await SendAlertToApiAsync(alert);

                AlertReceived?.Invoke(alert);
            }
        }
    }

    private async Task SendAlertToApiAsync(Alert alert)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{_apiUrl}/api/alerts", alert);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Alert wysłany do API: {alert.AlertMessage}");
            }
            else
            {
                Console.WriteLine($"Błąd podczas wysyłania alertu do API: {response.StatusCode}");
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

 

    private bool ShouldProcessAlert(string sid, string srcIp, int seconds)
    {
        string key = $"{sid}:{srcIp}";

        if (_recentAlerts.TryGetValue(key, out DateTime lastAlertTime))
        {
            if ((DateTime.Now - lastAlertTime).TotalSeconds < seconds)
            {
                return false; // Ignoruj alert
            }
        }

        _recentAlerts[key] = DateTime.Now; // Zapisz czas ostatniego alertu
        return true; // Procesuj alert
    }

}
