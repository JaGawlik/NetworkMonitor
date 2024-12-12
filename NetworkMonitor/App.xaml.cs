using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using NetworkMonitor.Database;

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Process _snortProcess;

        public string DBConnectionString { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string host = "localhost";
            int port = 5432;
            string user = "postgres";
            string password = "postgres";
            string initialDatabase = "postgres";
            string targetDb = "ids_system";

            string snortLogPath = @"C:\Snort\log\alert.ids";
            string snortPath = @"C:\Snort\bin\snort.exe";
            string arguments = "-i 6 -c C:\\Snort\\etc\\snort.conf -l C:\\Snort\\log -A fast -N";

            _snortProcess = Snort.SnortManager.StartSnort(snortPath, arguments);

            if (_snortProcess == null)
            {
                Shutdown();
                return;
            }

            //Connection do bazowej bazy
            DBConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={targetDb}";

            // Tworzenie bazy i tabel
            string initialConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={initialDatabase}";
            DatabaseInit.EnsureDatabaseExists(DBConnectionString, targetDb);
            //DatabaseInit.CreateTables(DBConnectionString);

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
