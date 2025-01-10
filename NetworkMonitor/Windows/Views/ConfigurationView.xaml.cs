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
        }

        private void BrowseLogFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Log Files (*.log)|*.log|All Files (*.*)|*.*",
                Title = "Wybierz plik logów Snort"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var viewModel = DataContext as ConfigurationViewModel;
                if (viewModel != null)
                {
                    viewModel.LogFilePath = openFileDialog.FileName;
                }
            }
        }
    }


}
