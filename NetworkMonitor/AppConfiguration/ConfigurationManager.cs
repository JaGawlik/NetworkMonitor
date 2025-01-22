using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Text.Json;

namespace NetworkMonitor.AppConfiguration
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = "config.json";

        public static ConfigurationSettings Settings { get; private set; }

        static ConfigurationManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                Settings = JsonSerializer.Deserialize<ConfigurationSettings>(json) ?? new ConfigurationSettings();
            }
            else
            {
                Settings = new ConfigurationSettings();
                SaveSettings();
            }

            if(string.IsNullOrWhiteSpace(Settings.ApiUrl))
            {
                try
                {
                    Settings.ApiUrl = DiscoverApiAddress();
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas wykrywania API: {ex.Message}");
                }
            }
        }

        public static void SaveSettings()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        public static string GetSetting(string key)
        {
            return key switch
            {
                "LogFilePath" => Settings.SnortLogPath,
                "ApiAddress" => Settings.ApiUrl,
                "SnortInstallationPath" => Settings.SnortInstallationPath,
                "ConnectionString" => Settings.DatabaseSettings.ConnectionString,
                "Role" => Settings.Role,
                _ => throw new ArgumentException($"Nieznany klucz ustawienia: {key}")
            };
        }

        public static void SetSetting(string key, string value)
        {
            switch (key)
            {
                case "LogFilePath":
                    Settings.SnortLogPath = value;
                    break;
                case "ApiAddress":
                    Settings.ApiUrl = value;
                    break;
                case "SnortInstallationPath":
                    Settings.SnortInstallationPath = value; 
                    break;
                case "Role":
                    Settings.Role = value;
                    break;
                case "ConnectionString":
                    Settings.DatabaseSettings.ConnectionString = value;
                    break;
                default:
                    throw new ArgumentException($"Nieznany klucz ustawienia: {key}");
            }
        }
        public static string GetLocalIpAddress()
        {
            string hostName = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostName);

            foreach (var address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }

            throw new Exception("Nie znaleziono lokalnego adresu IPv4.");
        }

        public static string DiscoverApiAddress()
        {
            // Lista potencjalnych adresów do sprawdzenia
            //TU MOŻE BYC PROB
            var potentialAddresses = new[]
            {
                "https://localhost:7270",
                "http://127.0.0.1:7270",
                $"http://{GetLocalIpAddress()}:7270"
            };

            foreach (var address in potentialAddresses)
            {
                try
                {
                    using var client = new HttpClient();
                    var response = client.GetAsync($"{address}/api/health").Result; 
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Znaleziono API pod adresem: {address}");
                        return address;
                    }
                }
                catch
                {
                    // Szukanie kolejnego adresu
                }
            }

            // Jeśli nie znaleziono działającego API
            throw new Exception("Nie można znaleźć działającego API.");
        }
    }





    public class ConfigurationSettings
    {
        public string SnortLogPath { get; set; }
        public string ApiUrl { get; set; } 
        public string SnortInstallationPath { get; set; } 

        public string Role { get; set; } = "";

        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } 
    }
}
