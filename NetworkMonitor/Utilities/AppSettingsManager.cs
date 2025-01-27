using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkMonitor.Utilities
{
    public static class AppSettingsManager
    {
        private static readonly string AppSettingsPath = FindAppSettingsFile();

        public static void UpdateConnectionString(string connectionString)
        {
            try
            {
                string path = Path.GetFullPath(AppSettingsPath);

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Plik {path} nie istnieje.");
                }

                var json = File.ReadAllText(path);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);

                // Aktualizuj connection string
                jsonObj["ConnectionStrings"]["DefaultConnection"] = connectionString;

                // Zapisz zmiany do pliku
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(path, output);

                StartApiServer();
                //WaitForApiStartup();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania connection string: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private static string FindAppSettingsFile()
        {
            // Ustal bazowy katalog aplikacji
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Plik docelowy
            string targetFile = "appsettings.json";
            string expectedPath = Path.Combine(baseDirectory, targetFile);

            // Jeśli plik istnieje, zwracamy jego ścieżkę
            if (File.Exists(expectedPath))
            {
                return expectedPath;
            }

            // Jeśli plik nie istnieje, tworzymy domyślną wersję appsettings.json
            try
            {
                var defaultSettings = new
                {
                    ConnectionStrings = new { DefaultConnection = "" },
                    Logging = new
                    {
                        LogLevel = new
                        {
                            Default = "Information",
                            Microsoft_AspNetCore = "Warning"
                        }
                    },
                    AllowedHosts = "*"
                };

                // Serializacja i zapis pliku
                string json = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
                File.WriteAllText(expectedPath, json);

                MessageBox.Show($"Utworzono nowy plik appsettings.json w: {expectedPath}", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return expectedPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Nie udało się utworzyć pliku appsettings.json: {ex.Message}");
            }
        }


        public static void StartApiServer()
        {
            try
            {
                // Ścieżka do pliku AlertApiServer.exe w bieżącym katalogu wykonywalnym
                string apiExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AlertApiServer.exe");

                if (!File.Exists(apiExecutablePath))
                {
                    MessageBox.Show($"Nie znaleziono pliku API: {apiExecutablePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Konfiguracja procesu
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = apiExecutablePath,
                    UseShellExecute = true, // Uruchamia proces w nowym oknie konsoli
                    CreateNoWindow = false // Widoczna konsola
                };

                // Start procesu
                Process.Start(processStartInfo);

                MessageBox.Show("API zostało uruchomione pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas uruchamiania API: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void WaitForApiStartup()
        {
            using var httpClient = new HttpClient();
            string apiUrl = "https://localhost:7270/api/health";

            for (int i = 0; i < 10; i++) // Próby co 1 sekundę przez maks. 10 sekund
            {
                try
                {
                    var response = httpClient.GetAsync(apiUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("API jest gotowe do pracy.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }

            MessageBox.Show("API nie uruchomiło się poprawnie w oczekiwanym czasie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
