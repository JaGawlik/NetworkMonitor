﻿using NetworkMonitor.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Utilities
{
    public static class ThresholdConfigManager
    {
        private static readonly string ThresholdFilePath = Path.Combine(AppConfiguration.ConfigurationManager.GetSetting("SnortInstallationPath"),"etc", "threshold.conf");

        public static List<AlertFilterRule> LoadRules()
        {
            var rules = new List<AlertFilterRule>();

            if (!File.Exists(ThresholdFilePath))
            {
                Console.WriteLine($"Plik {ThresholdFilePath} nie istnieje. Tworzenie nowego pliku.");
                return rules;
            }

            var lines = File.ReadAllLines(ThresholdFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 10 && parts[0] == "event_filter")
                {
                    var sidIndex = Array.FindIndex(parts, p => p.StartsWith("sig_id"));
                    var countIndex = Array.FindIndex(parts, p => p.StartsWith("count"));
                    var secondsIndex = Array.FindIndex(parts, p => p.StartsWith("seconds"));

                    if (sidIndex != -1 && countIndex != -1 && secondsIndex != -1)
                    {
                        var sid = parts[sidIndex + 1];
                        var count = int.Parse(parts[countIndex + 1]);
                        var seconds = int.Parse(parts[secondsIndex + 1]);

                        rules.Add(new AlertFilterRule
                        {
                            Sid = sid,
                            SourceIp = "by_src", // Domyślne trackowanie IP źródłowego
                            TimeLimitSeconds = seconds
                        });
                    }
                }
                else if (parts.Length >= 5 && parts[0] == "suppress")
                {
                    var sidIndex = Array.FindIndex(parts, p => p.StartsWith("sig_id"));
                    if (sidIndex != -1)
                    {
                        var sid = parts[sidIndex + 1];

                        rules.Add(new AlertFilterRule
                        {
                            Sid = sid,
                            SourceIp = parts.Length > 7 ? parts[7] : "",
                            TimeLimitSeconds = 0
                        });
                    }
                }
            }

            return rules;
        }


        public static void SaveRules(List<AlertFilterRule> rules)
        {
            var lines = new List<string>
            {
                "# Configure Thresholding and Suppression",
                "# ======================================",
                "# Auto-generated by NetworkMonitor",
                ""
            };

            foreach (var rule in rules)
            {
                // Reguły zapisane jako `event_filter` lub `suppress`
                if (rule.TimeLimitSeconds > 0)
                {
                    lines.Add($"event_filter gen_id 1, sig_id {rule.Sid}, type limit, track by_src, count 1, seconds {rule.TimeLimitSeconds}");
                }
                else
                {
                    lines.Add($"suppress gen_id 1, sig_id {rule.Sid}");
                }
            }

            File.WriteAllLines(ThresholdFilePath, lines);
            Console.WriteLine($"Zapisano {rules.Count} reguł do {ThresholdFilePath}");
        }

        public static void AddSuppressRule(int sigId, string trackBy, string ip = null, int? port = null)
        {
            var rule = $"suppress gen_id 1, sig_id {sigId}, track {trackBy}";
            if (!string.IsNullOrWhiteSpace(ip))
            {
                rule += $", ip {ip}";
            }
            if (port.HasValue)
            {
                rule += $":{port}";
            }

            File.AppendAllText(ThresholdFilePath, rule + Environment.NewLine);
            Console.WriteLine($"Dodano regułę suppress: {rule}");
        }

        public static void AddEventFilterRule(int sigId, string trackBy, string ip, int? port, int count, int seconds)
        {
            var rule = $"event_filter gen_id 1, sig_id {sigId}, type limit, track {trackBy}, count {count}, seconds {seconds}";
            if (!string.IsNullOrWhiteSpace(ip))
            {
                rule += $", ip {ip}";
            }
            if (port.HasValue)
            {
                rule += $":{port}";
            }

            File.AppendAllText(ThresholdFilePath, rule + Environment.NewLine);
            Console.WriteLine($"Dodano regułę event_filter: {rule}");
        }

        public static bool RuleExists(string rule)
        {
            if (!File.Exists(ThresholdFilePath))
            {
                return false;
            }

            var existingRules = File.ReadAllLines(ThresholdFilePath);
            return existingRules.Any(line => line.Trim() == rule.Trim());
        }


    }
}

