using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                //Aktualizuj connection string
                jsonObj["ConnectionStrings"]["DefaultConnection"] = connectionString;

                // Zapisz zmiany do pliku
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(path, output);

                MessageBox.Show("Connection string został zapisany do appsettings.json.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania connection string: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private static string FindAppSettingsFile()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string targetFile = "appsettings.json";

            // Ścieżka, gdzie plik powinien być tworzony
            string expectedPath = Path.Combine(currentDirectory, targetFile);

            // Jeśli plik istnieje, zwróć jego ścieżkę
            if (File.Exists(expectedPath))
            {
                return expectedPath;
            }

            // Jeśli plik nie istnieje, utwórz domyślny plik appsettings.json
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

                string json = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
                File.WriteAllText(expectedPath, json);

                MessageBox.Show("Utworzono nowy plik appsettings.json z domyślnymi ustawieniami.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return expectedPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Nie udało się utworzyć pliku appsettings.json: {ex.Message}");
            }
        }


    }
}
