using System;
using System.Windows;
using System.Windows.Controls;

namespace NetworkMonitor.Windows.Views
{
    public partial class AdminSettingsView : UserControl
    {
        public AdminSettingsView()
        {
            InitializeComponent();
            var viewModel = new AdminSettingsViewModel();
            DataContext = viewModel;

            Loaded += async (_, _) => await viewModel.LoadFrequentAlertsAsync();
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                // Dodaj tutaj logikę zapisywania reguł
                MessageBox.Show("Reguły zostały zapisane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddSelectedAlertRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel && viewModel.SelectedFrequentAlert != null)
            {
                var selectedAlert = viewModel.SelectedFrequentAlert;
                // Dodaj regułę na podstawie zaznaczonego alertu
                string sid = selectedAlert.Sid;
                string srcIp = ""; // Dodaj logikę ustawienia IP
                int limit = 60; // Domyślny limit czasu

                // Wyświetl informację dla użytkownika
                MessageBox.Show($"Dodano regułę: SID={sid}, IP={srcIp}, Limit={limit}s", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Nie wybrano alertu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
