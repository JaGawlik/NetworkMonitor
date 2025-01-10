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
        public Process StartSnort(string connectionString)
        {
            string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
            string snortLogPath = Path.Combine(snortInstallationPath, "log", "alert.ids");
            string snortPath = Path.Combine(snortInstallationPath, "bin", "snort.exe");
            string arguments = $"-i 6 -c {Path.Combine(snortInstallationPath, "etc", "snort.conf")} -l {Path.Combine(snortInstallationPath, "log")} -A fast -N";
            string apiUrl = "http://localhost:5136";

            var snortProcess = SnortManager.StartSnort(snortPath, arguments);

            if (snortProcess == null)
            {
                MessageBox.Show("Nie udało się uruchomić Snorta. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var monitor = new SnortAlertMonitor(snortLogPath, apiUrl);
            Task.Run(() => monitor.StartMonitoringAsync());

            return snortProcess;
        }
    }
}
