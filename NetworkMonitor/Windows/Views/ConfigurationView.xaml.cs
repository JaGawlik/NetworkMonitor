using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NetworkMonitor.AppConfiguration;

namespace NetworkMonitor.Windows.Views
{
    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
            DataContext = new ConfigurationViewModel();

            if (DataContext == null)
            {
                Console.WriteLine("DataContext nie jest ustawione w ConfigurationView");
            }
            
        }

        private void BrowseLogFile_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ConfigurationViewModel;
            if (viewModel == null)
            {
                Console.WriteLine("DataContext is null or not of type ConfigurationViewModel");
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "IDS Files (*.ids)|*.ids|Log Files (*.log)|*.log|All Files (*.*)|*.*",
                Title = "Wybierz plik logów Snort .ids"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (viewModel != null)
                {
                    viewModel.LogFilePath = openFileDialog.FileName;
                }
            }
        }

        private void BrowseSnortFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "Wybierz folder instalacyjny Snort",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                var viewModel = DataContext as ConfigurationViewModel;
                if (viewModel != null)
                {
                    viewModel.SnortInstallationPath = dialog.SelectedPath; // Aktualizuje model widoku
                }
            }
        }

        private void SaveSettings_Clikc(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ConfigurationViewModel;
            if (viewModel != null)
            {
                viewModel.SaveSettings();
                MessageBox.Show("Zapisano ustawienia", "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfigurationViewModel viewModel)
            {
                var passwordBox = sender as PasswordBox;
                viewModel.Password = passwordBox.Password;
            }
        }
        private async void FindApiButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ConfigurationViewModel;
            if (viewModel != null)
            {
                try
                {
                    string apiUrl = await ConfigurationManager.DiscoverApiAddressAsync();
                    ConfigurationManager.Settings.ApiUrl = apiUrl;
                    viewModel.ApiAddress = apiUrl;
                    MessageBox.Show($"Znaleziono API pod adresem: {apiUrl}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nie udało się znaleźć API: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadDevices_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfigurationViewModel viewModel)
            {
                viewModel.LoadDevices();
            }
        }
        private void SaveAndStartSnort_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ConfigurationViewModel;
            if (viewModel != null)
            {
                try
                {
                    // Sprawdzenie poprawności ustawień
                    if (string.IsNullOrEmpty(viewModel.SnortInstallationPath) || !Directory.Exists(viewModel.SnortInstallationPath))
                    {
                        MessageBox.Show("Ścieżka instalacji Snort jest nieprawidłowa lub nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(viewModel.LogFilePath) || !File.Exists(viewModel.LogFilePath))
                    {
                        MessageBox.Show("Ścieżka do pliku logów Snort jest nieprawidłowa lub plik nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(viewModel.ApiAddress))
                    {
                        MessageBox.Show("Adres API nie może być pusty.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var previousSettings = new
                    {
                        SnortInstallationPath = ConfigurationManager.Settings.SnortInstallationPath,
                        LogFilePath = ConfigurationManager.Settings.SnortLogPath,
                        ApiAddress = ConfigurationManager.Settings.ApiUrl
                    };

                    bool isConfigurationChanged =
                        !string.Equals(previousSettings.SnortInstallationPath, viewModel.SnortInstallationPath, StringComparison.Ordinal) ||
                        !string.Equals(previousSettings.LogFilePath, viewModel.LogFilePath, StringComparison.Ordinal) ||
                        !string.Equals(previousSettings.ApiAddress, viewModel.ApiAddress, StringComparison.Ordinal);


                    // Zapisanie konfiguracji
                    viewModel.SaveSettings();

                    if (isConfigurationChanged)
                    {
                        MessageBox.Show("Zmiany w konfiguracji Snort zostały zapisane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    var mainWindowViewModel = Application.Current.MainWindow.DataContext as MainWindowViewModel;
                    mainWindowViewModel?.InitializeSnortAndMonitoring();

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas uruchamiania Snort: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Błąd: brak powiązanego modelu widoku konfiguracji.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
