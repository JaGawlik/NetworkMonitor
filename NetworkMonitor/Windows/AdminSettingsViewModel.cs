using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Model;

namespace NetworkMonitor.Windows
{
    public class AdminSettingsViewModel : INotifyPropertyChanged
    {
        // Kolekcja najczęstszych alertów
        public ObservableCollection<FrequentAlert> FrequentAlerts { get; set; } = new ObservableCollection<FrequentAlert>();

        private FrequentAlert _selectedFrequentAlert;
        public FrequentAlert SelectedFrequentAlert
        {
            get => _selectedFrequentAlert;
            set
            {
                _selectedFrequentAlert = value;
                Console.WriteLine($"SelectedFrequentAlert zmieniony na: {value?.Sid}"); // Debug
                OnPropertyChanged();
            }
        }

        public AdminSettingsViewModel()
        {
            FrequentAlerts = new ObservableCollection<FrequentAlert>();
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
                    Sid = alert.Sid,
                    Message = alert.Message,
                    Count = alert.Count
                });
            }

            Console.WriteLine($"FrequentAlerts Count: {FrequentAlerts.Count}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FrequentAlert
    {
        public string Sid { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
    }
}
