using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.AppConfiguration;

namespace NetworkMonitor.Snort
{
    public class SnortManagerService
    {
        private static SnortManagerService _instance;
        public static SnortManagerService Instance => _instance ??= new SnortManagerService();

        private Process _snortProcess;
        private bool _isRunning = false;

        public bool IsSnortRunning => _isRunning;

        public Process StartSnort()
        {
            if (_isRunning)
            {
                Console.WriteLine("Snort jest już uruchomiony.");
                return _snortProcess;
            }

            ConfigurationManager.UpdateSnortConfig();
            bool isValid = ConfigurationManager.ValidateSnortConfig();
            if (!isValid)
            {
                Console.WriteLine("Snort.conf zawiera błędy.");
                return null;
            }

            string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
            string snortLogPath = Path.Combine(snortInstallationPath, "log");
            string snortPath = Path.Combine(snortInstallationPath, "bin", "snort.exe");
            string snortConfPath = Path.Combine(snortInstallationPath, "etc", "snort.conf");
            int? selectedIndex = ConfigurationManager.Settings.SelectedDevice?.Index;
            string arguments = $"-i {selectedIndex} -c {snortConfPath} -l {snortLogPath} -A fast -N";

            

            if (selectedIndex == null || string.IsNullOrEmpty(snortInstallationPath) || !File.Exists(snortPath))
            {
                Console.WriteLine("Brak poprawnej konfiguracji Snorta.");
                return null;
            }

            _snortProcess = StartSnortProcess(snortPath, arguments);
            Console.WriteLine($"Snort uruchamiany z argumentami: {arguments}");
            _isRunning = _snortProcess != null;

            return _snortProcess;
        }

        private Process StartSnortProcess(string snortPath, string arguments)
        {
            try
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

                var process = new Process { StartInfo = startInfo };

                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Snort]: {e.Data}"); };
                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Snort Error]: {e.Data}"); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                Console.WriteLine("Snort został uruchomiony.");
                return process;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas uruchamiania Snorta: {ex.Message}");
                return null;
            }
        }

        public void StopSnort()
        {
            if (!_isRunning)
            {
                Console.WriteLine("Snort nie jest uruchomiony.");
                return;
            }
                        
            Process[] snortProcesses = Process.GetProcessesByName("snort");

            foreach (var process in snortProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                    Console.WriteLine("Snort został zatrzymany.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas zatrzymywania Snorta: {ex.Message}");
                }
            }

            Instance._snortProcess.Kill();
            Instance._snortProcess.WaitForExit();

            _isRunning = false;
        }

        public void RestartSnort()
        {
            StopSnort();
            Task.Delay(1000).Wait();
            StartSnort();
        }
    }
}
