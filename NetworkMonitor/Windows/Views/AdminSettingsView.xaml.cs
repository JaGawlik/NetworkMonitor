using NetworkMonitor.Snort;
using NetworkMonitor.Utilities;
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

        private void RestartSnort_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                try
                {
                    viewModel.RestartSnort();
                    MessageBox.Show("Snort został zrestartowany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas restartu Snorta: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddLocalRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                try
                {
                    string action = (ActionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                    string protocol = (ProtocolComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                    string sourceIp = SourceIpTextBox.Text;
                    string sourcePort = SourcePortTextBox.Text;
                    string direction = (DirectionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                    string destinationIp = DestinationIpTextBox.Text;
                    string destinationPort = DestinationPortTextBox.Text;
                    string message = MessageTextBox.Text;
                    int sid = int.TryParse(SidTextBox.Text, out int parsedSid) ? parsedSid : 0;
                    int rev = int.TryParse(RevTextBox.Text, out int parsedRev) ? parsedRev : 1;

                    if (sid <= 1000000)
                    {
                        ShowError("SID musi być liczbą większą od 1000000.");
                        return;
                    }

                    viewModel.AddLocalRule(action, protocol, sourceIp, sourcePort, direction, destinationIp, destinationPort, message, sid, rev);
                    viewModel.SaveLocalRules();
                    MessageBox.Show($"Dodano regułę: SID={sid}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    HideError();
                }
                catch (Exception ex)
                {
                    ShowError($"Błąd: {ex.Message}");
                }
            }
        }

        private void RemoveLocalRule_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel && sender is Button button && button.DataContext is SnortLocalRule rule)
            {
                viewModel.RemoveLocalRule(rule.Sid);
                MessageBox.Show($"Usunięto regułę: SID={rule.Sid}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
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
