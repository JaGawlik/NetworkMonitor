using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Utilities;

namespace NetworkMonitor.Windows
{
    public class AdminSettingsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AlertFilterRule> Rules { get; set; } = new ObservableCollection<AlertFilterRule>();

        private ObservableCollection<FrequentAlert> _frequentAlerts = new ObservableCollection<FrequentAlert>();
        public ObservableCollection<FrequentAlert> FrequentAlerts
        {
            get => _frequentAlerts;
            set
            {
                _frequentAlerts = value;
                OnPropertyChanged();
            }
        }

        // Wybrany alert
        private FrequentAlert _selectedFrequentAlert;
        public FrequentAlert SelectedFrequentAlert
        {
            get => _selectedFrequentAlert;
            set
            {
                _selectedFrequentAlert = value;
                OnPropertyChanged();
            }
        }


        public AdminSettingsViewModel()
        {           
            FrequentAlerts = new ObservableCollection<FrequentAlert>();
            LoadRulesFromConfig();
            Console.WriteLine($"ViewModel stworzony. Początkowa liczba alertów: {FrequentAlerts.Count}");
        }

        // Załaduj najczęstsze alerty
        public async Task LoadFrequentAlertsAsync()
        {
            var snortAlertMonitor = new SnortAlertMonitor(System.Windows.Application.Current.Dispatcher);
            var alerts = await snortAlertMonitor.GetFrequentAlertsAsync();
            
            FrequentAlerts.Clear();

            foreach (var alert in alerts)
            {
                FrequentAlerts.Add(new FrequentAlert
                {
                    Sid = alert.Sid.ToString(),
                    Message = alert.Message,
                    Count = alert.Count
                });
            }
            Console.WriteLine($"Liczba alertów w FrequentAlerts: {FrequentAlerts.Count}");
        }

        public void AddRule(string sid, string sourceIp, int timeLimit)
        {
            if (Rules.Any(r => r.Sid == sid))
            {
                MessageBox.Show("Reguła z tym SID już istnieje!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = sourceIp,
                TimeLimitSeconds = timeLimit
            };

            Rules.Add(rule);
            Console.WriteLine($"Dodano regułę: SID={sid}, TimeLimit={timeLimit}s");
        }

        public void LoadRulesFromConfig()
        {
            var rules = ThresholdConfigManager.LoadRules();
            Rules.Clear();

            foreach (var rule in rules)
            {
                Rules.Add(rule);
            }

            Console.WriteLine($"Załadowano {Rules.Count} reguł z threshold.conf");
        }
        public void SaveRules()
        {
            ThresholdConfigManager.SaveRules(Rules.ToList());
            Console.WriteLine("Reguły zostały zapisane.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
