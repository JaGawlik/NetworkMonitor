using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;
using NetworkMonitor.Windows;
using NetworkMonitor.Configuration;
using NetworkMonitor.Model;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        public string DBConnectionString { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Console.WriteLine($"Unhandled exception: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"Unhandled exception: {ex.Message}\n{ex.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                Console.WriteLine($"UI exception: {args.Exception.Message}\n{args.Exception.StackTrace}");
                MessageBox.Show($"UI exception: {args.Exception.Message}\n{args.Exception.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            var roleSelectionWindow = new RoleSelectionWindow();
            if (roleSelectionWindow.ShowDialog() == true)
            {
                string selectedRole = roleSelectionWindow.SelectedRole;

                if (selectedRole == "Administrator")
                {
                    StartAsAdmin();
                }
                else if (selectedRole == "User")
                {
                    StartAsClient();
                }
                else
                {
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }
        }

        private Process _snortProcess;
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

        private void StartAsAdmin()
        {
            DBConnectionString = ConfigurationManager.GetSetting("ConnectionString");
            if (string.IsNullOrEmpty(DBConnectionString))
            {
                // Ustaw domyślny ConnectionString i zapisz
                ConfigurationManager.SetSetting("ConnectionString", "Host=localhost;Port=5432;Database=ids_system;Username=postgres;Password=postgres");
                DBConnectionString = ConfigurationManager.GetSetting("ConnectionString");
            }

            var databaseService = new DatabaseInitializerService();
            DatabaseInit.EnsureDatabaseExists(DBConnectionString, "ids_system");
            if (!databaseService.EnsureUsersExist(DBConnectionString))
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(new User { Role = "Administrator" });
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void StartAsClient()
        {
            string apiUrl = ConfigurationManager.GetSetting("ApiAddress");
            string logFilePath = ConfigurationManager.GetSetting("LogFilePath");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(logFilePath))
            {
                MessageBox.Show("Skonfiguruj API i ścieżkę do logów Snort.", "Błąd konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var snortMonitor = new SnortAlertMonitor(logFilePath, apiUrl);
            Task.Run(() => snortMonitor.StartMonitoringAsync());

            var mainWindow = new MainWindow(new User { Role = "User" });
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
