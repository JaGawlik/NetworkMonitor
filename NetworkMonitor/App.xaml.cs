using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;
using NetworkMonitor.Windows;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private Process _snortProcess;
        public string DBConnectionString { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    Console.WriteLine($"Unhandled exception: {ex.Message}\n{ex.StackTrace}");
                    System.Windows.MessageBox.Show($"Unhandled exception: {ex.Message}\n{ex.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                Console.WriteLine($"UI exception: {args.Exception.Message}\n{args.Exception.StackTrace}");
                System.Windows.MessageBox.Show($"UI exception: {args.Exception.Message}\n{args.Exception.StackTrace}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            //var roleSectionWindow = new RoleSelectionWindow();
            //if (roleSectionWindow.ShowDialog() == true)
            //{
            //    string selectedRole = roleSectionWindow.SelectedRole;

            //    if (selectedRole == "Administrator")
            //    {

            //    }
            //}

            // Inicjalizacja bazy danych i sprawdzanie użytkowników
            var databaseService = new DatabaseInitializerService();
            DBConnectionString = databaseService.InitializeDatabase("localhost", 5432, "postgres", "postgres", "postgres", "ids_system");

            if (!databaseService.EnsureUsersExist(DBConnectionString))
            {
                Shutdown();
                return;
            }

            // Uruchamianie Snorta
            var snortService = new SnortManagerService();
            _snortProcess = snortService.StartSnort(DBConnectionString);

            if (_snortProcess == null)
            {
                Shutdown();
                return;
            }

            // Otwieranie głównego okna
            var mainWindow = new MainWindow(null, DBConnectionString);
            System.Windows.Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
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
