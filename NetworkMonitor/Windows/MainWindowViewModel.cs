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
using NetworkMonitor.Utilities;
using System.Globalization;
using System.Windows.Data;

namespace NetworkMonitor
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int _lastMaxId = 0;
        private DispatcherTimer _timer;
        public ICommand UpdateAlertStatusCommand { get; }
        public RelayCommand<object> SearchAlertsByIpCommand { get; }

        private Process _snortProcess;
        private SnortManagerService _snortManagerService;
        private SnortAlertMonitor _snortAlertMonitor;

        private bool _isSnortInitialized = false;

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser ??= new User { Role = "User", Username = "Niezalogowany" };
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

        private bool _isAdminLoggedIn = false;
        public bool IsAdminLoggedIn
        {
            get => _isAdminLoggedIn;
            set
            {
                _isAdminLoggedIn = value;
                OnPropertyChanged(nameof(IsAdminLoggedIn));
            }
        }

        private string _searchSourceIp;
        public string SearchSourceIp
        {
            get => _searchSourceIp;
            set
            {
                if (_searchSourceIp != value)
                {
                    _searchSourceIp = value;
                    OnPropertyChanged(nameof(SearchSourceIp));

                    if (string.IsNullOrWhiteSpace(_searchSourceIp))
                    {
                        ResetSearch();
                    }
                }
            }
        }

        private ObservableCollection<AlertGroupViewModel> _alertGroupViewModels = new ObservableCollection<AlertGroupViewModel>();
        public ObservableCollection<AlertGroupViewModel> AlertGroupViewModels
        {
            get => _alertGroupViewModels;
            set
            {
                _alertGroupViewModels = value;
                OnPropertyChanged(nameof(AlertGroupViewModels));
            }
        }

        private string _localIp = ConfigurationManager.GetLocalIpAddress();
        private bool _isSearching = false;
        private readonly AlertRepository _alertRepository;
        public MainWindowViewModel(User user)
        {
            CurrentUser = new User { Role = "Guest", Username = "Niezalogowany" };

            SearchAlertsByIpCommand = new RelayCommand<object>(_ => SearchAlertsByIp());
            UpdateAlertStatusCommand = new RelayCommand<int>(async (alertId) => await UpdateAlertStatus(alertId, "resolved"));


            if (!IsConfigurationValid())
            {
                MessageBox.Show("Brak wymaganej konfiguracji. Przejdź do zakładki konfiguracji, aby uzupełnić dane.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);

                SelectedTabIndex = 1;
                return;
            }

            string apiUrl = ConfigurationManager.GetSetting("ApiAddress");
            _alertRepository = new AlertRepository(apiUrl);

            InitializeSnortAndMonitoring();

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
                MessageBox.Show("Snort jest już uruchomiony i gotowy do działania.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                string snortLogPath = ConfigurationManager.GetSetting("SnortInstallationPath") + @"\log\alert.ids";
                string apiUrl = ConfigurationManager.GetSetting("ApiAddress");

                _snortManagerService = new SnortManagerService();
                _snortAlertMonitor = new SnortAlertMonitor(Application.Current.Dispatcher);
                _snortAlertMonitor.AlertReceived += OnAlertReceived;

                StartSnortAndMonitorLogs();
                _isSnortInitialized = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas uruchamiania Snort: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async void LoadAlerts()
        {
            if (_isSearching || _alertRepository == null)
            {
                Console.WriteLine("Nie można załadować alertów, ponieważ `_alertRepository` jest null lub trwa wyszukiwanie.");
                return;
            }

            try
            {
                List<Alert> alerts = CurrentUser.Role switch
                {
                    "Guest" => await _alertRepository.GetAlertsAsync(ip: _localIp),
                    "Administrator" => await _alertRepository.GetAlertsAsync(),
                    _ => throw new InvalidOperationException("Nieznana rola użytkownika.")
                };

                Console.WriteLine($"Ładowanie wszystkich alertów, bez filtrowania statusu.");
                // Załaduj listę ignorowanych SID-ów z threshold.conf (jeśli dotyczy)
                var ignoredSids = ThresholdConfigManager.LoadRules()
                                                       .Where(rule => rule.TimeLimitSeconds == 0) // suppress rules
                                                       .Select(rule => rule.Sid)
                                                       .ToHashSet();

                // Filtruj alerty - usuń z listy alerty ignorowane na podstawie SID
                alerts = alerts.Where(alert => !ignoredSids.Contains(alert.SignatureId.ToString())).ToList();

                Console.WriteLine($"Po usunięciu ignorowanych SID-ów: {alerts.Count} alertów");
                //TUTAJ

                // Grupa i wyświetlenie alertów
                AlertGroupViewModels.Clear();
                GroupAndDisplayAlerts(alerts);

                // Powiadomienie widoku o zmianach
                OnPropertyChanged(nameof(AlertGroupViewModels));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania alertów: {ex.Message}");
            }
        }


        //Ładowanie wszystkich alertów
        //public async void LoadAlerts()
        //{
        //    if (_isSearching || _alertRepository == null)
        //    {
        //        Console.WriteLine("Nie można załadować alertów, ponieważ `_alertRepository` jest null lub trwa wyszukiwanie.");
        //        return;
        //    }

        //    try
        //    {
        //        List<Alert> alerts = CurrentUser.Role switch
        //        {
        //            "Guest" => await _alertRepository.GetAlertsAsync(ip: _localIp),
        //            "Administrator" => await _alertRepository.GetAlertsAsync(),
        //            _ => throw new InvalidOperationException("Nieznana rola użytkownika.")
        //        };

        //        GroupAndDisplayAlerts(alerts);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Błąd podczas ładowania alertów: {ex.Message}");
        //    }
        //}


        private void GroupAndDisplayAlerts(List<Alert> alerts)
        {
            var groupedAlerts = alerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new
                {
                    DestinationIp = group.Key,
                    Alerts = group.ToList()
                });

            foreach (var group in groupedAlerts)
            {
                var existingGroup = AlertGroupViewModels.FirstOrDefault(g => g.DestinationIp == group.DestinationIp);

                if (existingGroup != null)
                {
                    foreach (var alert in group.Alerts)
                    {
                        if (!existingGroup.Alerts.Any(a => a.Id == alert.Id))
                        {
                            existingGroup.Alerts.Add(alert);
                        }
                    }
                }
                else
                {
                    AlertGroupViewModels.Add(new AlertGroupViewModel
                    {
                        DestinationIp = group.DestinationIp,
                        Alerts = new ObservableCollection<Alert>(group.Alerts),
                        IsExpanded = false
                    });
                }
            }

            for (int i = AlertGroupViewModels.Count - 1; i >= 0; i--)
            {
                var group = AlertGroupViewModels[i];
                if (!groupedAlerts.Any(g => g.DestinationIp == group.DestinationIp))
                {
                    AlertGroupViewModels.RemoveAt(i);
                }
            }
        }

        private async Task UpdateAlertStatus(int alertId, string newStatus)
        {
            try
            {
                using var httpClient = new HttpClient { BaseAddress = new Uri(ConfigurationManager.GetSetting("ApiAddress")) };
                var response = await httpClient.PutAsJsonAsync($"/api/alerts/{alertId}/status", newStatus);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Zaktualizowano status alertu o ID {alertId} na {newStatus}");

                    // Usuń alert z listy, jeśli status zmieniono na "resolved"
                    foreach (var group in AlertGroupViewModels.ToList())
                    {
                        var alertToRemove = group.Alerts.FirstOrDefault(a => a.Id == alertId);
                        if (alertToRemove != null)
                        {
                            group.Alerts.Remove(alertToRemove);

                            // Usuń grupę, jeśli nie ma w niej alertów
                            if (!group.Alerts.Any())
                            {
                                AlertGroupViewModels.Remove(group);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Błąd aktualizacji alertu: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas aktualizacji alertu: {ex.Message}");
            }
        }


        private void SortGroupsByLatestAlert()
        {
            var sortedGroups = AlertGroupViewModels
                .OrderByDescending(g => g.Alerts.Max(a => a.Timestamp))
                .ToList();

            AlertGroupViewModels.Clear();
            foreach (var group in sortedGroups)
            {
                AlertGroupViewModels.Add(group);
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
            else if (SelectedTabIndex == 2)
            {
                if (IsAdminLoggedIn)
                {
                    CurrentView = new AdminSettingsView
                    {
                        DataContext = new AdminSettingsViewModel()
                    };
                }
                else
                {
                    MessageBox.Show("Nie masz uprawnień do tego widoku. Zaloguj się jako administrator.", "Brak dostępu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SelectedTabIndex = 0; // Przełączenie na widok domyślny
                }
            }
        }

        private void StartSnortAndMonitorLogs() //Starwowanie snorta
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
                if (CurrentUser.Role == "Guest" && alert.DestinationIp != _localIp)
                {
                    return;
                }

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
            });
        }

        private async void CheckForNewAlerts(object sender, EventArgs e)
        {
            if (_isSearching)
            {
                return;
            }

            try
            {
                var allAlerts = CurrentUser.Role switch
                {
                    "Guest" => await _alertRepository.GetAlertsAsync(ip: _localIp),
                    "Administrator" => await _alertRepository.GetAlertsAsync(),
                    _ => throw new InvalidOperationException("Nieznana rola użytkownika.")
                };

                GroupAndDisplayAlerts(allAlerts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas sprawdzania nowych alertów: {ex.Message}");
            }
        }


        private async void SearchAlertsByIp()
        {
            if (string.IsNullOrWhiteSpace(SearchSourceIp))
            {
                MessageBox.Show("Wprowadź poprawny adres IP", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isSearching = true; // Zablokuj odświeżanie
                var alerts = await _alertRepository.GetAlertsAsync();

                var filteredAlerts = alerts
                .Where(a => RemovePort(a.SourceIp) == RemovePort(SearchSourceIp) ||
                            RemovePort(a.DestinationIp) == RemovePort(SearchSourceIp))
                .ToList();


                GroupAndDisplayAlerts(filteredAlerts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wyszukiwania alertów: {ex.Message}");
            }
        }

        private string RemovePort(string ipWithPort)
        {
            if (string.IsNullOrWhiteSpace(ipWithPort))
                return string.Empty;

            var parts = ipWithPort.Split(':'); // Podział na IP i port
            return parts[0]; // Zwracamy tylko IP
        }


        private void ResetSearch()
        {
            if (_isSearching)
            {
                _isSearching = false;
                LoadAlerts();
            }
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

        public class StatusToEnabledConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                string status = value as string;
                return status != "resolved"; 
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        //FILTR STATUSÓW
        //private string _selectedStatusFilter = "all";
        //public string SelectedStatusFilter
        //{
        //    get => _selectedStatusFilter;
        //    set
        //    {
        //        if (_selectedStatusFilter != value)
        //        {
        //            _selectedStatusFilter = value;
        //            Console.WriteLine($"SelectedStatusFilter ustawiony na: {_selectedStatusFilter}");
        //            OnPropertyChanged(nameof(SelectedStatusFilter));
        //            LoadAlerts();
        //        }
        //    }
        //}

        //public async void LoadAlerts()
        //{
        //    if (_isSearching || _alertRepository == null)
        //    {
        //        Console.WriteLine("Nie można załadować alertów, ponieważ `_alertRepository` jest null lub trwa wyszukiwanie.");
        //        return;
        //    }

        //    try
        //    {
        //        List<Alert> alerts = CurrentUser.Role switch
        //        {
        //            "Guest" => await _alertRepository.GetAlertsAsync(ip: _localIp),
        //            "Administrator" => await _alertRepository.GetAlertsAsync(),
        //            _ => throw new InvalidOperationException("Nieznana rola użytkownika.")
        //        };


        //        // Filtruj alerty na podstawie wybranego statusu
        //        if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "all")
        //        {
        //            alerts = alerts.Where(alert => alert.Status?.Trim().Equals(SelectedStatusFilter.Trim(), StringComparison.OrdinalIgnoreCase) == true).ToList();
        //        }

        //        Console.WriteLine($"Po filtrowaniu: {alerts.Count} alertów");

        //        // Załaduj listę ignorowanych SID-ów z threshold.conf (jeśli dotyczy)
        //        var ignoredSids = ThresholdConfigManager.LoadRules()
        //                                               .Where(rule => rule.TimeLimitSeconds == 0) // suppress rules
        //                                               .Select(rule => rule.Sid)
        //                                               .ToHashSet();

        //        // Filtruj alerty - usuń z listy alerty ignorowane na podstawie SID
        //        alerts = alerts.Where(alert => !ignoredSids.Contains(alert.SignatureId.ToString())).ToList();

        //        Console.WriteLine($"Po usunięciu ignorowanych SID-ów: {alerts.Count} alertów");

        //        // Grupa i wyświetlenie alertów
        //        AlertGroupViewModels.Clear();
        //        GroupAndDisplayAlerts(alerts);

        //        // Powiadomienie widoku o zmianach
        //        OnPropertyChanged(nameof(AlertGroupViewModels));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Błąd podczas ładowania alertów: {ex.Message}");
        //    }
        //}
    }
}