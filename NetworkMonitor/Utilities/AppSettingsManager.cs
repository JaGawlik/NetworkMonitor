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

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                string potentialPath = Path.Combine(currentDirectory, "AlertApiServer", targetFile);
                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            throw new FileNotFoundException("Nie znaleziono pliku appsettings.json.");
        }

    }
}
