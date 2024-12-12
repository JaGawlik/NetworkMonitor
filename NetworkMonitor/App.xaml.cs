using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Model;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private Process _snortProcess;

        public string DBConnectionString { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string host = "localhost";
            int port = 5432;
            string dbUser = "postgres";
            string password = "postgres";
            string initialDatabase = "postgres";
            string targetDb = "ids_system";

            DBConnectionString = $"Host={host};Port={port};Username={dbUser};Password={password};Database={targetDb}";

            // Tworzenie bazy danych
            string initialConnectionString = $"Host={host};Port={port};Username={dbUser};Password={password};Database={initialDatabase}";
            DatabaseInit.EnsureDatabaseExists(initialConnectionString, targetDb);

            // Otwarcie okna logowania
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                var user = loginWindow.LoggedUser;

                // Uruchamianie Snorta i monitorowanie alertów po zalogowaniu
                InitializeSnortAndMonitoring();

                // Otwieranie głównego okna
                var mainWindow = new MainWindow(user, DBConnectionString);
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }

        private void InitializeSnortAndMonitoring()
        {
            string snortLogPath = @"C:\Snort\log\alert.ids";
            string snortPath = @"C:\Snort\bin\snort.exe";
            string arguments = "-i 6 -c C:\\Snort\\etc\\snort.conf -l C:\\Snort\\log -A fast -N";

            _snortProcess = Snort.SnortManager.StartSnort(snortPath, arguments);

            if (_snortProcess == null)
            {
                MessageBox.Show("Nie udało się uruchomić Snorta. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var monitor = new Snort.SnortAlertMonitor(snortLogPath, DBConnectionString);
            Task.Run(() => monitor.StartMonitoringAsync());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (_snortProcess != null && !_snortProcess.HasExited)
            {
                try
                {
                    _snortProcess.Kill();
                    _snortProcess.WaitForExit();
                    Console.WriteLine("Snort został zatrzymany.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd podczas zatrzymywania Snorta: {ex.Message}");
                }
            }
        }
    }
}
