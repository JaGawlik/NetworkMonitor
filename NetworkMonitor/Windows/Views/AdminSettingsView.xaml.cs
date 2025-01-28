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

                if (!viewModel.RuleExists(alert.Sid)) // Sprawdzenie, czy reguła już istnieje
                {
                    viewModel.AddRule(alert.Sid, "", 60);
                    viewModel.SaveRules(); // Automatyczne zapisanie reguły
                    MessageBox.Show($"Dodano i zapisano regułę: SID={alert.Sid}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Reguła dla SID={alert.Sid} już istnieje!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void AddSuppressRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                string sid = SidInput.Text;
                string track = (TrackComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string ip = IpInput.Text;
                int? port = int.TryParse(PortInput.Text, out int parsedPort) ? parsedPort : null;

                if (string.IsNullOrWhiteSpace(sid) || !int.TryParse(sid, out _))
                {
                    ShowError("SID musi być liczbą!");
                    return;
                }

                try
                {
                    if (!viewModel.RuleExists(sid, ip)) // Sprawdzenie, czy reguła już istnieje
                    {
                        viewModel.AddSuppressRule(sid, track, ip, port);
                        viewModel.SaveRules(); // Automatyczne zapisanie reguły
                        MessageBox.Show("Reguła ignorująca została dodana i zapisana!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        HideError();
                    }
                    else
                    {
                        ShowError($"Reguła suppress dla SID={sid} i IP={ip} już istnieje!");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Błąd: {ex.Message}");
                }
            }
        }

        private void AddEventFilterRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                string sid = SidInput.Text;
                string track = (TrackComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string ip = IpInput.Text;
                int? port = int.TryParse(PortInput.Text, out int parsedPort) ? parsedPort : null;

                if (string.IsNullOrWhiteSpace(sid) || !int.TryParse(sid, out _))
                {
                    ShowError("SID musi być liczbą!");
                    return;
                }

                try
                {
                    if (!viewModel.RuleExists(sid, ip)) // Sprawdzenie, czy reguła już istnieje
                    {
                        viewModel.AddEventFilterRule(sid, track, ip, port, count: 1, seconds: 60);
                        viewModel.SaveRules(); // Automatyczne zapisanie reguły
                        MessageBox.Show("Reguła ograniczająca została dodana i zapisana!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        HideError();
                    }
                    else
                    {
                        ShowError($"Reguła event_filter dla SID={sid} i IP={ip} już istnieje!");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Błąd: {ex.Message}");
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
        }
    }
}
