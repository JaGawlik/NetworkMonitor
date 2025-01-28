using System.Collections.ObjectModel;
using System.Windows;

namespace NetworkMonitor.Windows
{
    public class AdminSettingsViewModel
    {
        public ObservableCollection<AlertFilterRule> Rules { get; set; } = new ObservableCollection<AlertFilterRule>();

        public void AddRule(string sid, string srcIp, int limit)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(srcIp) || limit <= 0)
            {
                MessageBox.Show("Wszystkie pola muszą być poprawnie wypełnione.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = srcIp,
                TimeLimitSeconds = limit
            };

            Rules.Add(rule);
        }

        public void SaveRules()
        {
            // Zapisz reguły do konfiguracji lub pliku
            MessageBox.Show("Reguły zostały zapisane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class AlertFilterRule
    {
        public string Sid { get; set; }
        public string SourceIp { get; set; }
        public int TimeLimitSeconds { get; set; }
    }
}
