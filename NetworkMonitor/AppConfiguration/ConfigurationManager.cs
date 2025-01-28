using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

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
                    SaveSettings();
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
                "SelectedInterfaceIndex" => Settings.SelectedDevice.Index.ToString(),
                "HomeNet" => Settings.HomeNet,
                "LogDir" => Settings.LogDir,
                "RulePath" => Settings.RulePath,
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
                case "SelectedInterfaceIndex":
                    if (int.TryParse(value, out int index))
                    {
                        Settings.SelectedDevice.Index = index;
                    }
                    break;
                case "HomeNet":
                    Settings.HomeNet = value;
                    break;
                case "LogDir":
                    Settings.LogDir = value;
                    break;
                case "RulePath":
                    Settings.RulePath = value;
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
                    var response = await client.GetAsync($"{address}/api/health");
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

        public static void UpdateSnortConfig()
        {
            try
            {
                string snortConfPath = Path.Combine(Settings.SnortInstallationPath, "etc", "snort.conf");

                if (!File.Exists(snortConfPath))
                {
                    throw new FileNotFoundException($"Plik konfiguracyjny Snorta nie istnieje: {snortConfPath}");
                }

                string[] lines = File.ReadAllLines(snortConfPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    // Aktualizacja HOME_NET
                    if (lines[i].StartsWith("ipvar HOME_NET"))
                    {
                        lines[i] = $"ipvar HOME_NET {Settings.HomeNet}";
                    }

                    // Aktualizacja logdir
                    if (lines[i].StartsWith("config logdir:"))
                    {
                        lines[i] = $"config logdir: {Path.Combine(Settings.SnortInstallationPath, Settings.LogDir)}";
                    }

                    // Aktualizacja RULE_PATH
                    if (lines[i].StartsWith("var RULE_PATH"))
                    {
                        lines[i] = $"var RULE_PATH {Path.Combine(Settings.SnortInstallationPath, Settings.RulePath)}";
                    }
                }

                File.WriteAllLines(snortConfPath, lines);
                Console.WriteLine("Plik snort.conf został pomyślnie zaktualizowany.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas aktualizacji pliku snort.conf: {ex.Message}");
            }
        }

        public static bool ValidateSnortConfig()
        {
            try
            {
                string snortPath = Path.Combine(Settings.SnortInstallationPath, "bin", "snort.exe");
                string snortConfPath = Path.Combine(Settings.SnortInstallationPath, "etc", "snort.conf");
                int selectedInterfaceIndex = Settings.SelectedDevice?.Index ?? -1;

                if (!File.Exists(snortPath))
                {
                    MessageBox.Show($"Nie znaleziono pliku Snort.exe pod ścieżką: {snortPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!File.Exists(snortConfPath))
                {
                    MessageBox.Show($"Nie znaleziono pliku konfiguracyjnego Snorta pod ścieżką: {snortConfPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (selectedInterfaceIndex == -1)
                {
                    MessageBox.Show("Nie wybrano interfejsu sieciowego do testowania Snorta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Przygotowanie argumentów
                string arguments = $"-i {selectedInterfaceIndex} -c \"{snortConfPath}\" -T";

                var startInfo = new ProcessStartInfo
                {
                    FileName = snortPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };

                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                process.OutputDataReceived += (sender, args) => { if (args.Data != null) outputBuilder.AppendLine(args.Data); };
                process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errorBuilder.AppendLine(args.Data); };

                process.Start();

                // Uruchomienie asynchronicznego odbioru danych
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Czekaj na zakończenie procesu
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Plik snort.conf został pomyślnie zweryfikowany.");
                    MessageBox.Show("Konfiguracja Snorta jest poprawna.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    Console.WriteLine($"Błąd w snort.conf: {errorBuilder}");
                    MessageBox.Show($"Błąd w pliku snort.conf:\n{errorBuilder}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas weryfikacji konfiguracji Snorta: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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
            public SelectedDeviceDetails SelectedDevice { get; set; }
            public string HomeNet { get; set; } = "192.168.0.0/24"; // Domyślnie lokalna sieć
            public string LogDir { get; set; } = "log";             // Domyślny folder logów
            public string RulePath { get; set; } = "rules";
        }

        public class SelectedDeviceDetails
        {
            public int Index { get; set; }
            public string DeviceName { get; set; }
            public string IpAddress { get; set; }
        }
    }
}
