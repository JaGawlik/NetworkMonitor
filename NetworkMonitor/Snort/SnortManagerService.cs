using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Snort;

namespace NetworkMonitor.Snort
{
    public class SnortManagerService
    {
        public Process StartSnort(string connectionString)
        {
            string snortLogPath = @"C:\Snort\log\alert.ids";
            string snortPath = @"C:\Snort\bin\snort.exe";
            string arguments = "-i 6 -c C:\\Snort\\etc\\snort.conf -l C:\\Snort\\log -A fast -N";
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
