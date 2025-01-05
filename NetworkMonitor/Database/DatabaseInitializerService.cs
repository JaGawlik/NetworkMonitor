using NetworkMonitor.Database;
using NetworkMonitor.Repository;

namespace NetworkMonitor.Database
{
    public class DatabaseInitializerService
    {
        public string InitializeDatabase(string host, int port, string username, string password, string initialDatabase, string targetDatabase)
        {
            string initialConnectionString = $"Host={host};Port={port};Username={username};Password={password};Database={initialDatabase}";
            string targetConnectionString = $"Host={host};Port={port};Username={username};Password={password};Database={targetDatabase}";

            DatabaseInit.EnsureDatabaseExists(initialConnectionString, targetDatabase);
            return targetConnectionString;
        }

        public bool EnsureUsersExist(string connectionString)
        {
            if (!UserRepository.HasUsers(connectionString))
            {
                var addUserWindow = new AddUserWindow(connectionString);
                return addUserWindow.ShowDialog() == true;
            }

            return true;
        }
    }
}
