using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;
using NetworkMonitor.Windows;
using NetworkMonitor.AppConfiguration;
using NetworkMonitor.Model;
using NetworkMonitor.Snort;
using System.Net;

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


            var role = ConfigurationManager.GetSetting("Role");

            if (string.IsNullOrEmpty(role))
            {
                var roleWindows = new RoleSelectionWindow();
                if (roleWindows.ShowDialog() == true)
                {
                    role = roleWindows.SelectedRole;
                    ConfigurationManager.SetSetting("Role", role);
                    ConfigurationManager.SaveSettings();
                }
            }

            if (role == "Administrator")
            {
                DatabaseService databaseService = new DatabaseService();
                databaseService.InitializeDatabase();

                if (!databaseService.EnsureUsersExist())
                {
                    Shutdown();
                    return;
                }

                StartProgram(role);
            }
            else if (role == "User")
            {
                StartProgram(role);
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

        //private void StartMainProgramAsAdministrator()
        //{
        //    try
        //    {
        //        var snortManager = new SnortManagerService();
        //        _snortProcess = snortManager.StartSnort();

        //        if (_snortProcess == null)
        //        {
        //            Shutdown();
        //            return;
        //        }

        //        // Utworzenie okna głównego
        //        var mainWindow = new MainWindow(new User { Role = "Administrator" });

        //        // Powiązanie danych (np. pobranie alertów z bazy danych)
        //        var alertRepository = new AlertRepository(ConfigurationManager.GetSetting("ApiAddress"));
        //        //mainWindow.DataContext = new AlertsViewModel(alertRepository.GetAlertsAsync());

        //        // Wyświetlenie okna
        //        MainWindow = mainWindow;
        //        mainWindow.Show();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Błąd podczas uruchamiania programu: {ex.Message}");
        //        MessageBox.Show($"Błąd podczas uruchamiania programu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        //        Shutdown();
        //    }
        //}

        private void StartProgram(string role)
        {
            var mainWindow = new MainWindow(new User { Role = role });
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
