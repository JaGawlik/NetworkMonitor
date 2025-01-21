using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Snort;
using NetworkMonitor.Configuration;

namespace NetworkMonitor.Snort
{
    public class SnortManagerService
    {
        public Process StartSnort()
        {
            string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
            string snortLogPath = Path.Combine(snortInstallationPath, "log", "alert.ids");
            string snortPath = Path.Combine(snortInstallationPath, "bin", "snort.exe");
            string arguments = $"-i 6 -c {Path.Combine(snortInstallationPath, "etc", "snort.conf")} -l {Path.Combine(snortInstallationPath, "log")} -A fast -N";
            string apiUrl = "http://localhost:5136";

            if (string.IsNullOrEmpty(snortInstallationPath))
            {
                MessageBox.Show("Ścieżka instalacji Snorta nie została skonfigurowana.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (!File.Exists(snortPath))
            {
                MessageBox.Show($"Nie znaleziono pliku Snort.exe w ścieżce: {snortPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // Start Snort process
            var snortProcess = StartSnortProcess(snortPath, arguments);
            if (snortProcess == null)
            {
                MessageBox.Show("Nie udało się uruchomić Snorta. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // Monitor Snort logs
            var monitor = new SnortAlertMonitor(snortLogPath, apiUrl);
            Task.Run(() => monitor.StartMonitoringAsync());

            return snortProcess;
        }

        private Process StartSnortProcess(string snortPath, string arguments)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = snortPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process snortProcess = new Process { StartInfo = startInfo };

            try
            {
                bool started = snortProcess.Start();
                if (started)
                {
                    Console.WriteLine("Snort został uruchomiony.");
                    return snortProcess;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas uruchamiania Snorta: {ex.Message}");
            }

            Console.WriteLine("Nie udało się uruchomić Snorta.");
            return null;
        }

        //public Process StartSnort()
        //{
        //    string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
        //    string snortLogPath = Path.Combine(snortInstallationPath, "log", "alert.ids");
        //    string snortPath = Path.Combine(snortInstallationPath, "bin", "snort.exe");
        //    string arguments = $"-i 6 -c {Path.Combine(snortInstallationPath, "etc", "snort.conf")} -l {Path.Combine(snortInstallationPath, "log")} -A fast -N";
        //    string apiUrl = "http://localhost:5136";

        //    string localIpAddress = ConfigurationManager.GetLocalIpAddress();

        //    var snortProcess = SnortManager.StartSnort(snortPath, arguments);

        //    if (snortProcess == null)
        //    {
        //        MessageBox.Show("Nie udało się uruchomić Snorta. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return null;
        //    }

        //    if (string.IsNullOrEmpty(snortInstallationPath))
        //    {
        //        MessageBox.Show("Ścieżka instalacji Snorta nie została skonfigurowana.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return null;
        //    }

        //    if (!File.Exists(snortPath))
        //    {
        //        MessageBox.Show($"Nie znaleziono pliku Snort.exe w ścieżce: {snortPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return null;
        //    }

        //    var monitor = new SnortAlertMonitor(snortLogPath, apiUrl);
        //    Task.Run(() => monitor.StartMonitoringAsync());

        //    return snortProcess;
        //}
    }
}
