using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NetworkMonitor.Configuration;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;
using NetworkMonitor.Windows.Views;

namespace NetworkMonitor
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int _lastMaxId = 0;
        private DispatcherTimer _timer;

        public ICommand UpdateAlertStatusCommand { get; }

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

        private int _selectedTabIndex;
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

        public ObservableCollection<AlertGroup> AlertGroups { get; set; } = new ObservableCollection<AlertGroup>();

        public string ConnectionString { get; }

        private string _localIp = ConfigurationManager.GetLocalIpAddress();

        private readonly AlertRepository _alertRepository;
        public MainWindowViewModel(User user)
        {
            CurrentUser = user ?? new User { Role = "guest", Username = "Niezalogowany" };
            _alertRepository = new AlertRepository(ConfigurationManager.GetSetting("ApiAddress"));

            UpdateAlertStatusCommand = new RelayCommand<int>(async (alertId) => await UpdateAlertStatus(alertId, "resolved"));

            SelectedTabIndex = 0;

            LoadAlerts();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += CheckForNewAlerts;
            _timer.Start();
        }

        public async void LoadAlerts()
        {
            try
            {
                List<Alert> alerts = CurrentUser.Role switch
                {
                    "guest" => await _alertRepository.GetAlertsAsync(ip: _localIp),
                    "user" when !string.IsNullOrEmpty(CurrentUser.AssignedIp) => await _alertRepository.GetAlertsAsync(assignedIp: CurrentUser.AssignedIp),
                    _ => await _alertRepository.GetAlertsAsync()
                };

                GroupAndDisplayAlerts(alerts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania alertów: {ex.Message}");
            }
        }

        private void GroupAndDisplayAlerts(List<Alert> alerts)
        {
            var groupedAlerts = alerts
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

        private async void CheckForNewAlerts(object sender, EventArgs e)
        {
            try
            {
                // Pobierz nowe alerty z API
                var newAlerts = await _alertRepository.GetAlertsAsync();

                // Filtrowanie alertów w zależności od roli użytkownika
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
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas sprawdzania nowych alertów: {ex.Message}");
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
                var configViewModel = new ConfigurationViewModel();
                CurrentView = new ConfigurationView
                {
                    DataContext = configViewModel
                };
            }
        }

        public async Task UpdateAlertStatus(int alertId, string newStatus)
        {
            try
            {
                await _alertRepository.UpdateAlertStatusAsync(alertId, newStatus);
                Console.WriteLine($"Zaktualizowano status alertu o ID {alertId} na {newStatus}");

                LoadAlerts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas aktualizacji alertu: {ex.Message}");
                MessageBox.Show($"Błąd podczas aktualizacji alertu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
