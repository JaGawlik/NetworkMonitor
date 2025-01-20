using System;
using System.IO;
using System.Net;
using System.Runtime;
using System.Text.Json;

namespace NetworkMonitor.Configuration
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
