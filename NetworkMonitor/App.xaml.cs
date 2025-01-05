using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private Process _snortProcess;
        public string DBConnectionString { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Obsługa wyjątków globalnych
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
            Application.Current.MainWindow = mainWindow;
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
