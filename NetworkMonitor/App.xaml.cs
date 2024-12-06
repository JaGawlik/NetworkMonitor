using System.Configuration;
using System.Data;
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
            int port = 5432;
            string user = "postgres";
            string password = "secure_password";
            string initialDatabase = "postgres";
            string targetDb = "ids_system";

            // Connection string do bazy bazowej
            string initialConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={initialDatabase}";
            // Connection string do docelowej bazy
            string targetConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={targetDb}";

            // Tworzymy bazę jeśli nie istnieje
            DatabaseInit.EnsureDatabaseExists(initialConnectionString, targetDb);

            // Tworzymy tabele
            DatabaseInit.CreateTables(targetConnectionString);
        }
    }

}
