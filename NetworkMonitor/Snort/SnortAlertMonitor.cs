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
             @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[(?<sid>\d+:\d+:\d+)\]\s(?<message>.*?)\s\[\*\*\](\s\[Classification:\s.*?\])?\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>\w+)\}\s(?<srcip>[a-fA-F0-9\:\.]+):\d+\s->\s(?<dstip>[a-fA-F0-9\:\.]+):\d+",
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

    public List<(string Sid, string Message, int Count)> GetFrequentAlerts(int topN = 10)
    {
        if (!File.Exists(_snortLogPath))
        {
            Console.WriteLine($"Plik logów Snorta nie istnieje: {_snortLogPath}");
            return new List<(string Sid, string Message, int Count)>();
        }

        var alertCounts = new Dictionary<string, (string Message, int Count)>();

        foreach (var line in File.ReadLines(_snortLogPath))
        {
            // Przykład logu: [**] [1:12345:1] Alert Message [**]
            var match = _regex.Match(line);
            if (!match.Success) continue;

            string sid = match.Groups["sid"].Value; // Możesz dostosować grupy w regex
            string message = match.Groups["message"].Value;

            if (!alertCounts.ContainsKey(sid))
            {
                alertCounts[sid] = (message, 0);
            }

            alertCounts[sid] = (alertCounts[sid].Message, alertCounts[sid].Count + 1);
        }

        return alertCounts
            .OrderByDescending(kvp => kvp.Value.Count)
            .Take(topN)
            .Select(kvp => (Sid: kvp.Key, Message: kvp.Value.Message, Count: kvp.Value.Count))
            .ToList();
    }

    public void DisplayFrequentAlerts(int topN = 10)
    {
        var frequentAlerts = GetFrequentAlerts(topN);
        Console.WriteLine("Najczęstsze alerty w logach Snorta:");
        foreach (var alert in frequentAlerts)
        {
            Console.WriteLine($"SID: {alert.Sid}, Wiadomość: {alert.Message}, Liczba wystąpień: {alert.Count}");
        }
    }

    private Dictionary<string, DateTime> _recentAlerts = new();

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
