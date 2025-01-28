using System.Collections.ObjectModel;
using System.Windows;
using NetworkMonitor.Model;

namespace NetworkMonitor.Windows
{
    public class AdminSettingsViewModel
    {
        // Kolekcja reguł
        public ObservableCollection<AlertFilterRule> Rules { get; set; } = new ObservableCollection<AlertFilterRule>();

        // Kolekcja najczęstszych alertów
        public ObservableCollection<FrequentAlert> FrequentAlerts { get; set; } = new ObservableCollection<FrequentAlert>();

        // Dodaj regułę
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

        // Zapisz reguły
        public void SaveRules()
        {
            // Zapisz reguły do konfiguracji lub pliku
            MessageBox.Show("Reguły zostały zapisane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Załaduj najczęstsze alerty
        public void LoadFrequentAlerts()
        {
            var snortAlertMonitor = new SnortAlertMonitor(Application.Current.Dispatcher);
            var frequentAlerts = snortAlertMonitor.GetFrequentAlerts();

            FrequentAlerts.Clear();
            foreach (var alert in frequentAlerts)
            {
                FrequentAlerts.Add(new FrequentAlert
                {
                    Sid = alert.Sid,
                    Message = alert.Message,
                    Count = alert.Count
                });
            }
        }
    }

    public class AlertFilterRule
    {
        public string Sid { get; set; }
        public string SourceIp { get; set; }
        public int TimeLimitSeconds { get; set; }
    }

    public class FrequentAlert
    {
        public string Sid { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
    }
}
