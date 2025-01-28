using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
            LoadRulesFromConfig();
        }

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
        }

        public void AddRule(string sid, string sourceIp, int timeLimit)
        {
            if (RuleExists(sid)) // Sprawdzenie przed dodaniem
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
        }


        public bool RuleExists(string sid, string ip = null)
        {
            return Rules.Any(r => r.Sid == sid && (ip == null || r.SourceIp == ip));
        }

        public void LoadRulesFromConfig()
        {
            var rules = ThresholdConfigManager.LoadRules();
            Rules.Clear();

            foreach (var rule in rules)
            {
                Rules.Add(rule);
            }
        }

        public void SaveRules()
        {
            ThresholdConfigManager.SaveRules(Rules.ToList());
        }

        public void AddSuppressRule(string sid, string track, string ip = null, int? port = null)
        {
            if (RuleExists(sid, ip))
            {
                throw new Exception($"Reguła suppress dla SID={sid} i IP={ip} już istnieje!");
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = ip,
                TimeLimitSeconds = 0
            };

            Rules.Add(rule);
            ThresholdConfigManager.AddSuppressRule(int.Parse(sid), track, ip, port);
        }

        public void AddEventFilterRule(string sid, string track, string ip, int? port, int count, int seconds)
        {
            if (RuleExists(sid, ip))
            {
                throw new Exception($"Reguła event_filter dla SID={sid} i IP={ip} już istnieje!");
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = ip,
                TimeLimitSeconds = seconds
            };

            Rules.Add(rule);
            ThresholdConfigManager.AddEventFilterRule(int.Parse(sid), track, ip, port, count, seconds);
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
