﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;
using NetworkMonitor.Windows;
using NetworkMonitor.AppConfiguration;
using NetworkMonitor.Model;
using System.Net;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
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

            // Ładowanie ustawień z pliku (lub tworzenie pliku, jeśli nie istnieje)
            await ConfigurationManager.LoadSettingsAsync();

            // Pobieramy rolę z pliku konfiguracji
            var role = ConfigurationManager.GetSetting("Role");

            if (string.IsNullOrEmpty(role))
            {
                // Wyświetlamy okno wyboru roli, jeśli nie jest ustawiona
                var roleWindows = new RoleSelectionWindow();
                if (roleWindows.ShowDialog() == true)
                {
                    role = roleWindows.SelectedRole;
                    ConfigurationManager.SetSetting("Role", role);
                    ConfigurationManager.SaveSettings();
                }

                if (role == "Administrator")
                {
                    DatabaseService databaseService = new DatabaseService();
                    databaseService.InitializeDatabase();

                    if (!await databaseService.EnsureUsersExistAsync())
                    {
                        MessageBox.Show("Nie udało się dodać administratora. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown(); // Zamykamy aplikację, jeśli dodanie użytkownika nie powiodło się
                        return;
                    }

                    await ConfigurationManager.InitializeApiUrlAsync();


                    StartProgram(role);
                }
                else if (role == "User")
                {
                    // Logika dla roli User
                    await ConfigurationManager.InitializeApiUrlAsync();
                    StartProgram(role);
                }
            }
            else
            {
                StartProgram(role);
            }
        }    
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            SnortManagerService.Instance.StopSnort();
            Console.WriteLine("Snort został zatrzymany.");
        }

        private void StartProgram(string role)
        {
            SnortManagerService.Instance.StartSnort();
            var mainWindow = new MainWindow(new User { Role = role });
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
