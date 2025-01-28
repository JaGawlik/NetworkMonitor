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

            Loaded += async (_, _) =>
            {
                Console.WriteLine("Ładowanie najczęstszych alertów...");

                if (DataContext is AdminSettingsViewModel viewModel)
                {
                    await viewModel.LoadFrequentAlertsAsync();
                    Console.WriteLine("Najczęstsze alerty załadowane.");
                }
                else
                {
                    Console.WriteLine("DataContext nie jest typu AdminSettingsViewModel.");
                }
            };
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                viewModel.SaveRules();
                MessageBox.Show("Reguły zostały zapisane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddSelectedAlertRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel && viewModel.SelectedFrequentAlert != null)
            {
                var alert = viewModel.SelectedFrequentAlert;
                viewModel.AddRule(alert.Sid, "", 60);
                MessageBox.Show($"Dodano regułę: SID={alert.Sid}", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
