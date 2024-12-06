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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string host = "localhost";
            int port = 5433;
            string user = "postgres";
            string password = "postgres";
            string initialDatabase = "postgres";
            string targetDb = "ids_system";

            string snortLogPath = @"C:\Snort\log\alert.ids";
            string snortPath = @"C:\Snort\bin\snort.exe";
            string arguments = "-i 6 -c C:\\Snort\\etc\\snort.conf -l C:\\Snort\\log -A fast -N";

            bool snortStartded = Snort.SnortManager.StartSnort(snortPath, arguments);

            if (!snortStartded)
            {
                MessageBox.Show("Nie udało się uruchomić Snorta. Aplikacja zostanie zamknięta.");
                Application.Current.Shutdown();
                return;
            }



            //Connection do bazowej bazy
            string initialConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={initialDatabase}";
            //Connectiondo docelowej bazy
            string targetConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={targetDb}";

            //Tworzenie bazy gdy nie istnieje
            DatabaseInit.EnsureDatabaseExists(initialConnectionString, targetDb);

            //Tworzymy tabele
            DatabaseInit.CreateTables(targetConnectionString);

            var monitor = new Snort.SnortAlertMonitor(snortLogPath, targetConnectionString);
            Task.Run(() => monitor.StartMonitoringAsync());

        }
    }

}
