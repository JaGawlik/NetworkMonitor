using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetworkMonitor.Model;

namespace NetworkMonitor.Snort
{
    internal class SnortAlertMonitor
    {
        private string _snortLogPath;
        private string _apiUrl; 
        private Regex _regex;

        public SnortAlertMonitor(string logPath, string apiUrl)
        {
            _snortLogPath = logPath;
            _apiUrl = apiUrl;
            _regex = new Regex(
                @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[\d+:\d+:\d+\]\s(?<message>.*?)\s\[\*\*\]\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>\w+)\}\s(?<srcip>[\d\.]+)\s->\s(?<dstip>[\d\.]+)",
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
                // Odczyt od końca pliku
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
                    else
                    {
                        // Sprawdzanie po chwili
                        await Task.Delay(1000);
                    }
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
                    // Wyślij alert do API
                    await SendAlertToApiAsync(new Alert
                    {
                        Timestamp = timestamp,
                        AlertMessage = alertMessage,
                        SourceIp = srcIp,
                        DestinationIp = dstIp,
                        Protocol = protocol,
                        Status = "new",
                        SnortInstance = "Snort_PC_01"
                    });
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
    }
}
