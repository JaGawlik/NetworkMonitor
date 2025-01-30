using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Snort;
using NetworkMonitor.Utilities;

namespace NetworkMonitor.Windows
{
    public class AdminSettingsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<AlertFilterRule> ThresholdRules { get; set; } = new ObservableCollection<AlertFilterRule>();
        public ObservableCollection<SnortLocalRule> LocalRules { get; set; } = new ObservableCollection<SnortLocalRule>();

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
            LoadLocalRules();
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
            if (ThresholdRuleExists(sid)) // Sprawdzenie przed dodaniem
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

            ThresholdRules.Add(rule);
        }


        public bool ThresholdRuleExists(string sid, string ip = null)
        {
            return ThresholdRules.Any(r => r.Sid == sid && (ip == null || r.SourceIp == ip));
        }

        public void LoadRulesFromConfig()
        {
            var rules = ThresholdConfigManager.LoadRules();
            ThresholdRules.Clear();

            foreach (var rule in rules)
            {
                ThresholdRules.Add(rule);
            }
        }

        public void SaveRules()
        {
            ThresholdConfigManager.SaveRules(ThresholdRules.ToList());
            //LocalRulesConfigManager.SaveRules(LocalRules.ToList()); <- zobaczymym czy dziala
        }
        public void LoadLocalRules()
        {
            var rules = LocalRulesConfigManager.LoadLocalRules();
            LocalRules.Clear();

            foreach (var rule in rules)
            {
                LocalRules.Add(rule);
            }
        }
        public void SaveLocalRules()
        {
            LocalRulesConfigManager.SaveRules(LocalRules.ToList());
        }

        public void AddSuppressRule(string sid, string track, string ip = null, int? port = null)
        {
            if (ThresholdRuleExists(sid, ip))
            {
                throw new Exception($"Reguła suppress dla SID={sid} i IP={ip} już istnieje!");
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = ip,
                TimeLimitSeconds = 0
            };

            ThresholdRules.Add(rule);
            ThresholdConfigManager.AddSuppressRule(int.Parse(sid), track, ip, port);
        }

        public void AddEventFilterRule(string sid, string track, string ip, int? port, int count, int seconds)
        {
            if (ThresholdRuleExists(sid, ip))
            {
                throw new Exception($"Reguła event_filter dla SID={sid} i IP={ip} już istnieje!");
            }

            var rule = new AlertFilterRule
            {
                Sid = sid,
                SourceIp = ip,
                TimeLimitSeconds = seconds
            };

            ThresholdRules.Add(rule);
            ThresholdConfigManager.AddEventFilterRule(int.Parse(sid), track, ip, port, count, seconds);
        }

        public void AddLocalRule(string action, string protocol, string sourceIp, string sourcePort, string direciton, string destinationIp, string destinationPort, string message, int sid, int rev)
        {
            if(LocalRuleExists(sid))
            {
                MessageBox.Show($"Reguła z SID={sid} już istnieje!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LocalRulesConfigManager.AddRule(action, protocol, sourceIp, sourcePort, direciton, destinationIp, destinationPort, message, sid, rev);
            LoadLocalRules();
        }
        public void RemoveLocalRule(int sid)
        {
            LocalRulesConfigManager.RemoveRule(sid);
            LoadLocalRules();
        }
        public bool LocalRuleExists(int sid)
        {
            return LocalRules.Any(r => r.Sid == sid);
        }
        public void RestartSnort()
        {
            SnortManagerService.Instance.RestartSnort(); // <- Tu moze byc błąd
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
