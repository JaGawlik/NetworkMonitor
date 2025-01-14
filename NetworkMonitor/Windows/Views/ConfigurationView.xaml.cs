using System;
using System.Collections.Generic;
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
using NetworkMonitor.Configuration;

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
                //var viewModel = DataContext as ConfigurationViewModel;
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
    }


}
