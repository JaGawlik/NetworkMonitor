using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using NetworkMonitor.AppConfiguration;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;
using NetworkMonitor.Snort;
using NetworkMonitor.Windows;
using NetworkMonitor.Windows.Views;

namespace NetworkMonitor
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int _lastMaxId = 0;
        private DispatcherTimer _timer;        
        public ICommand UpdateAlertStatusCommand { get; }

        private Process _snortProcess;
        private SnortManagerService _snortManagerService;
        private SnortAlertMonitor _snortAlertMonitor;

        private bool _isSnortInitialized = false;

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

        private ObservableCollection<AlertGroupViewModel> _alertGroupViewModels;

        public ObservableCollection<AlertGroupViewModel> AlertGroupViewModels
        {
            get => _alertGroupViewModels;
            set
            {
                _alertGroupViewModels = value;
                OnPropertyChanged(nameof(AlertGroupViewModels));
            }
        }

        public ObservableCollection<AlertGroupViewModel> AlertGroups { get; set; } = new ObservableCollection<AlertGroupViewModel>();

        private string _localIp = ConfigurationManager.GetLocalIpAddress();

        private readonly AlertRepository _alertRepository;
        public MainWindowViewModel(User user)
        {
            CurrentUser = user ?? new User { Role = "User", Username = "Niezalogowany" };

            // Sprawdzanie, czy konfiguracja jest kompletna
            if (!IsConfigurationValid())
            {
                MessageBox.Show("Brak wymaganej konfiguracji. Przejdź do zakładki konfiguracji, aby uzupełnić dane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);

                // Przejdź do zakładki konfiguracji
                SelectedTabIndex = 1;
                return;
            }

            string apiUrl = ConfigurationManager.GetSetting("ApiAddress");
            _alertRepository = new AlertRepository(apiUrl);

            // Inicjalizacja Snort i powiązanych komponentów
            InitializeSnortAndMonitoring();

            // Inicjalizacja innych składników
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
        public void InitializeSnortAndMonitoring()
        {
            if (_isSnortInitialized)
            {
                return;
            }
            try
            {
                string snortLogPath = ConfigurationManager.GetSetting("SnortInstallationPath") + @"\log\alert.ids";
                string apiUrl = ConfigurationManager.GetSetting("ApiAddress");

                _snortManagerService = new SnortManagerService();
                _snortAlertMonitor = new SnortAlertMonitor(snortLogPath, apiUrl, Application.Current.Dispatcher);
                _snortAlertMonitor.AlertReceived += OnAlertReceived;

                StartSnortAndMonitorLogs();
                _isSnortInitialized = true;

                MessageBox.Show("Snort został poprawnie uruchomiony.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas uruchamiania Snort: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            //SaveExpandedStates();

            var groupedAlerts = alerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new AlertGroupViewModel
                {
                    DestinationIp = group.Key,
                    Alerts = new ObservableCollection<Alert>(group.ToList()), 
                    IsExpanded = false 
                });

            AlertGroupViewModels = new ObservableCollection<AlertGroupViewModel>(groupedAlerts);

           //RestoreExpandedStates();
        }


        private async void CheckForNewAlerts(object sender, EventArgs e)
        {
            try
            {
                var newAlerts = await _alertRepository.GetAlertsAsync();

                // Filtrowanie alertów w zależności od roli użytkownika
                if (CurrentUser.Role == "guest")
                {
                    newAlerts = newAlerts.Where(a => a.DestinationIp == _localIp).ToList();
                }

                if (newAlerts.Any())
                {
                    _lastMaxId = newAlerts.Max(a => a.Id);

                    foreach (var alert in newAlerts)
                    {
                        var existingGroup = AlertGroupViewModels.FirstOrDefault(g => g.DestinationIp == alert.DestinationIp);

                        if (existingGroup != null)
                        {
                            if (!existingGroup.Alerts.Any(a => a.Id == alert.Id))
                            {
                                existingGroup.Alerts.Add(alert);
                            }
                        }
                        else
                        {
                            AlertGroupViewModels.Add(new AlertGroupViewModel
                            {
                                DestinationIp = alert.DestinationIp,
                                Alerts = new ObservableCollection<Alert> { alert },
                                IsExpanded = false
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
                CurrentView = new ConfigurationView
                {
                    DataContext = new ConfigurationViewModel()
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

        public async Task<User> LoginUserAsync(string username, string password)
        { 
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri(ConfigurationManager.GetSetting("ApiAddress"));
                var credentials = new { Username = username, Password = password };

                var response = await client.PostAsJsonAsync("/api/auth/login", credentials);

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    return user;
                }
                else
                {
                    Console.WriteLine($"Błąd logowania: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas logowania: {ex.Message}");
            }

            return null;
        }

        private void StartSnortAndMonitorLogs()
        {
            try
            {
                _snortProcess = _snortManagerService.StartSnort();

                if (_snortProcess == null)
                {
                    MessageBox.Show("Nie udało się uruchomić Snorta. Sprawdź konfigurację.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Task.Run(() => _snortAlertMonitor.StartMonitoringAsync());

                Console.WriteLine("Snort i monitorowanie logów zostały uruchomione.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas uruchamiania Snorta: {ex.Message}");
            }
        }

        public void StopSnort()
        {
            if (_snortProcess != null && !_snortProcess.HasExited)
            {
                try
                {
                    _snortProcess.Kill();
                    _snortProcess.WaitForExit();
                    Console.WriteLine("Snort został zatrzymany.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas zatrzymywania Snorta: {ex.Message}");
                }
            }
        }

        private void OnAlertReceived(Alert alert)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingGroup = AlertGroupViewModels.FirstOrDefault(g => g.DestinationIp == alert.DestinationIp);
                if (existingGroup != null)
                {
                    existingGroup.Alerts.Add(alert);
                }
                else
                {
                    AlertGroupViewModels.Add(new AlertGroupViewModel
                    {
                        DestinationIp = alert.DestinationIp,
                        Alerts = new ObservableCollection<Alert> { alert },
                        IsExpanded = false
                    });
                }
            });
        }

        private bool IsConfigurationValid()
        {
            string snortPath = ConfigurationManager.GetSetting("SnortInstallationPath");
            string apiUrl = ConfigurationManager.GetSetting("ApiAddress");
            string snortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");

            return !string.IsNullOrEmpty(snortPath) && !string.IsNullOrEmpty(apiUrl) && !string.IsNullOrEmpty(snortInstallationPath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
