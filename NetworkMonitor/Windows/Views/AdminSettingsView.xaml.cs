using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using NetworkMonitor.AppConfiguration;

namespace NetworkMonitor.Windows.Views
{
    public partial class AdminSettingsView : UserControl
    {
        public AdminSettingsView()
        {
            InitializeComponent();
            var viewModel = new AdminSettingsViewModel();
            DataContext = viewModel;

            var snortAlertMonitror = new SnortAlertMonitor(Application.Current.Dispatcher);

            // Załaduj najczęstsze alerty przy inicjalizacji widoku
            var frequentAlerts = snortAlertMonitror.GetFrequentAlerts();

            foreach (var alert in frequentAlerts)
            {
                FrequentAlertsList.Items.Add(new
                {
                    Sid = alert.Sid,
                    Message = alert.Message,
                    Count = alert.Count
                });
            }
        }


        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                viewModel.SaveRules();
            }
        }

        private void AddSelectedAlertRule_Click(object sender, RoutedEventArgs e)
        {
            if (FrequentAlertsList.SelectedItem is FrequentAlert selectedAlert && DataContext is AdminSettingsViewModel viewModel)
            {
                // Dodaj regułę na podstawie zaznaczonego alertu
                string sid = selectedAlert.Sid;
                string srcIp = ""; // Możesz dodać logikę wyboru IP (np. domyślnie puste lub wybrane ręcznie)
                int limit = 60; // Domyślny limit czasu

                viewModel.AddRule(sid, srcIp, limit);
                MessageBox.Show($"Dodano regułę: SID={sid}, IP={srcIp}, Limit={limit}s", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Nie wybrano alertu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

}
