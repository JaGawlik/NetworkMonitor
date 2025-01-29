using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Snort;
using NetworkMonitor.AppConfiguration;

namespace NetworkMonitor.Snort
{
    public class SnortManagerService
    {
        //private static SnortManagerService _instance;
        //public static SnortManagerService Instance => _instance ??= new SnortManagerService();
        //public string selectedInterface { get; set; }
        public Process StartSnort()
        {
            ConfigurationManager.UpdateSnortConfig();
            bool isValid = ConfigurationManager.ValidateSnortConfig();
            if (!isValid)
            {
                Console.WriteLine("Snort.conf zawiera błędy.");
            }

            string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
            string snortLogPath = Path.Combine(snortInstallationPath, "log", "alert.ids");
            string snortPath = Path.Combine(snortInstallationPath, "bin", "snort.exe");
            string snortConfPath = Path.Combine(snortInstallationPath, "etc", "snort.conf");
            int? selectedIndex = ConfigurationManager.Settings.SelectedDevice?.Index;
            //string arguments = $"-i {selectedIndex} -c {Path.Combine(snortInstallationPath, "etc", "snort.conf")} -l {Path.Combine(snortInstallationPath, "log")} -A fast -N";
            string arguments = $"{selectedIndex} -c \"{snortConfPath}\" -l \"{snortLogPath}\" -A fast -N";
            string apiUrl = ConfigurationManager.GetSetting("ApiAddress");

            Console.WriteLine($"Snort uruchamiany z argumentami: {arguments}");

            if (selectedIndex == null)
            {
                MessageBox.Show("Nie wybrano urządzenia do monitorowania.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
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
            var monitor = new SnortAlertMonitor(Application.Current.Dispatcher);
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
                snortProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"[Snort]: {e.Data}");
                };

                snortProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"[Snort Error]: {e.Data}");
                };

                bool started = snortProcess.Start();
                if (started)
                {
                    snortProcess.BeginOutputReadLine(); 
                    snortProcess.BeginErrorReadLine();  
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



        private string ExecuteSnortCommand(string snortPath, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = snortPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }
    }
}
