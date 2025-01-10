using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;
using NetworkMonitor.Windows.Views;

namespace NetworkMonitor
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int _lastMaxId = 0;
        private DispatcherTimer _timer;

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser ??= new User { Role = "guest", Username = "Niezalogowany" };
            set
            {
                _currentUser = value;
                OnPropertyChanged(nameof(CurrentUser));
            }
        }

        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
                UpdateCurrentView();
            }
        }

        private int _selectedTabIndex;

        public ObservableCollection<AlertGroup> AlertGroups { get; set; } = new ObservableCollection<AlertGroup>();

        public string ConnectionString { get; }

        private string _localIp = GetLocalIpAddress();

        public MainWindowViewModel(User user, string connectionString)
        {
            CurrentUser = user ?? new User { Role = "guest", Username = "Niezalogowany" };
            ConnectionString = connectionString;

            SelectedTabIndex = 0;

            LoadAlerts();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += CheckForNewAlerts;
            _timer.Start();
        }

        public void LoadAlerts()
        {
            var allAlerts = AlertRepository.GetAlerts(ConnectionString) ?? new List<Alert>();

            switch (CurrentUser.Role)
            {
                case "guest":
                    allAlerts = allAlerts.Where(a => a.DestinationIp == _localIp).ToList();
                    break;

                case "user":
                    allAlerts = allAlerts.Where(a => a.DestinationIp == CurrentUser.AssignedIp).ToList();
                    break;
            }

            var groupedAlerts = allAlerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new AlertGroup
                {
                    DestinationIp = group.Key,
                    Alerts = group.ToList()
                });

            AlertGroups.Clear();
            foreach (var group in groupedAlerts)
            {
                AlertGroups.Add(group);
            }
        }

        private void CheckForNewAlerts(object sender, EventArgs e)
        {
            var newAlerts = AlertRepository.GetAlerts(ConnectionString)
                                           .Where(a => a.Id > _lastMaxId)
                                           .ToList();

            if (CurrentUser.Role == "guest")
            {
                newAlerts = newAlerts
                    .Where(a => a.DestinationIp == _localIp)
                    .ToList();
            }

            if (newAlerts.Any())
            {
                _lastMaxId = newAlerts.Max(a => a.Id);

                foreach (var alert in newAlerts)
                {
                    var existingGroup = AlertGroups.FirstOrDefault(g => g.DestinationIp == alert.DestinationIp);

                    if (existingGroup != null)
                    {
                        if (!existingGroup.Alerts.Any(a => a.Id == alert.Id))
                        {
                            existingGroup.Alerts.Add(alert);
                        }
                    }
                    else
                    {
                        AlertGroups.Add(new AlertGroup
                        {
                            DestinationIp = alert.DestinationIp,
                            Alerts = new List<Alert> { alert }
                        });
                    }
                }

                SortGroupsByLatestAlert();
            }
        }

        private void SortGroupsByLatestAlert()
        {
            var sortedGroups = AlertGroups
                .OrderByDescending(g => g.Alerts.Max(a => a.Timestamp))
                .ToList();

            AlertGroups.Clear();
            foreach (var group in sortedGroups)
            {
                AlertGroups.Add(group);
            }
        }

        private static string GetLocalIpAddress()
        {
            string hostName = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostName);

            foreach (var address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }

            throw new Exception("Nie znaleziono lokalnego adresu IPv4.");
        }
        private void UpdateCurrentView()
        {
            if (SelectedTabIndex == 0) 
            {
                CurrentView = new AlertsView
                {
                    DataContext = this
                };
            }
            else if (SelectedTabIndex == 1) 
            {
                CurrentView = new ConfigurationView(); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
