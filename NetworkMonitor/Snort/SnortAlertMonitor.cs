using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkMonitor.Snort
{
    internal class SnortAlertMonitor
    {
        private string _snortLogPath;
        private string _connString;
        private Regex _regex;

        public SnortAlertMonitor(string logPath, string connString)
        {
            _snortLogPath = logPath;
            _connString = connString;
            _regex = new Regex(
                @"(?<date>\d{2}/\d{2}-\d{2}:\d{2}:\d{2}\.\d+)\s+\[\*\*\]\s+\[\d+:\d+:\d+\]\s(?<message>.*?)\s\[\*\*\]\s\[Priority:\s(?<priority>\d+)\]\s\{(?<protocol>\w+)\}\s(?<srcip>[\d\.]+)\s->\s(?<dstip>[\d\.]+)",
                RegexOptions.Compiled
            );
        }

        public async Task StartMonitoringAsync()
        {
            using (FileStream fs = new FileStream(_snortLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                //odczyt ostatniego alertu
                fs.Seek(0, SeekOrigin.End);

                while (true)
                {
                    string line = await sr.ReadLineAsync();
                    if (line != null)
                    {
                        await ProcessLineAsync(line);
                    }
                    else
                    {
                        //sprawdzanie po chwili
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

                DateTime timestamp;
                if (DateTime.TryParseExact($"{currentYear}/{month}/{day} {hms}",
                    "yyyy/MM/dd HH:mm:ss.ffffff",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out timestamp))
                {
                    // Zapis do bazy
                    await InsertAlertAsync(timestamp, alertMessage, srcIp, dstIp, protocol);
                }
            }
        }

        private async Task InsertAlertAsync(DateTime timestamp, string message, string srcIp, string dstIp, string protocol)
        {
            using (var conn = new NpgsqlConnection(_connString))
            {
                await conn.OpenAsync();
                string insertQuery = @"
                INSERT INTO alerts (timestamp, alert_message, source_ip, destination_ip, protocol, snort_instance)
                VALUES (@time, @msg, @src, @dst, @proto, @inst)";

                using (var cmd = new NpgsqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("time", timestamp);
                    cmd.Parameters.AddWithValue("msg", message);
                    cmd.Parameters.AddWithValue("src", srcIp);
                    cmd.Parameters.AddWithValue("dst", dstIp);
                    cmd.Parameters.AddWithValue("proto", protocol);
                    cmd.Parameters.AddWithValue("inst", "Snort_PC_01");

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            Console.WriteLine($"Zapisano alert: {message} z {srcIp} do {dstIp} ({protocol})");
        }

    }
}
