using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkMonitor.AppConfiguration
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = "config.json";

        public static ConfigurationSettings Settings { get; private set; }

        /// <summary>
        /// Inicjalizacja konfiguracji aplikacji, uwzględniając różne role.
        /// </summary>
        public static async Task InitializeAsync(string userRole)
        {
            await LoadSettingsAsync();

            if (userRole == "Administrator")
            {
                Console.WriteLine("Administrator - API zostanie ustawione po inicjalizacji bazy danych.");
            }
            else
            {
                Console.WriteLine("Zwykły użytkownik - próba automatycznego ustawienia API.");
                await InitializeApiUrlAsync();
            }
        }

        /// <summary>
        /// Ładowanie ustawień z pliku konfiguracji.
        /// </summary>
        public static async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = await File.ReadAllTextAsync(ConfigFilePath);
                    Settings = JsonSerializer.Deserialize<ConfigurationSettings>(json) ?? new ConfigurationSettings();
                }
                else
                {
                    // Tworzymy plik konfiguracyjny z domyślnymi ustawieniami
                    Settings = new ConfigurationSettings();
                    SaveSettings(); // Zapisujemy domyślne ustawienia w pliku
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania konfiguracji: {ex.Message}");
                Settings = new ConfigurationSettings(); 
                SaveSettings(); 
            }
        }


        /// <summary>
        /// Zapisanie ustawień do pliku konfiguracji.
        /// </summary>
        public static void SaveSettings()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }

        /// <summary>
        /// Pobranie wartości ustawienia na podstawie klucza.
        /// </summary>
        public static string GetSetting(string key)
        {
            return key switch
            {
                "LogFilePath" => Settings.SnortLogPath,
                "ApiAddress" => Settings.ApiUrl,
                "SnortInstallationPath" => Settings.SnortInstallationPath,
                "Role" => Settings.Role,
                _ => throw new KeyNotFoundException($"Klucz ustawienia '{key}' nie istnieje w konfiguracji.")
            };
        }

        /// <summary>
        /// Ustawienie wartości konfiguracji.
        /// </summary>
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
                default:
                    throw new ArgumentException($"Nieznany klucz ustawienia: {key}");
            }
        }

        /// <summary>
        /// Pobranie lokalnego adresu IP.
        /// </summary>
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

        /// <summary>
        /// Odkrywanie adresu API.
        /// </summary>
        public static async Task<string> DiscoverApiAddressAsync()
        {
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
                    using var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    };
                    using var client = new HttpClient(handler);

                    Console.WriteLine($"Sprawdzanie adresu: {address}");
                    var response = await client.GetAsync($"{address}/api/alerts");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Znaleziono działające API pod adresem: {address}");
                        return address;
                    }
                }
                catch (Exception)
                {
                    // Ignorujemy błędy i przechodzimy do następnego adresu
                }
            }

            throw new Exception("Nie udało się znaleźć działającego API.");
        }

        /// <summary>
        /// Inicjalizacja adresu API.
        /// </summary>
        public static async Task InitializeApiUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(Settings.ApiUrl))
            {
                try
                {
                    Settings.ApiUrl = await DiscoverApiAddressAsync();
                    SaveSettings(); // Zapisujemy nowo wykryte API
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nie udało się znaleźć API: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Klasa przechowująca ustawienia aplikacji.
    /// </summary>
    public class ConfigurationSettings
    {
        public string SnortLogPath { get; set; }
        public string ApiUrl { get; set; }
        public string SnortInstallationPath { get; set; }
        public string Role { get; set; } = "";
    }
}
