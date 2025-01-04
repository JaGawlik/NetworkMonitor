using NetworkMonitor.Database;
using NetworkMonitor.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Alert> Alerts { get; set; }
        private string _connectionString;
        private DispatcherTimer _timer;
        private int _lastMaxId = 0;
        //private string localIp = GetLocalIpAddress();
        private string localIp = "192.168.0.5";

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser ??= new User { Role = "guest" };
            set => _currentUser = value;
           
        }

        public ObservableCollection<AlertGroup> AlertGroups { get; set; }

        public MainWindow(User user, string connectionString)
        {
            InitializeComponent();

            this.Closing += (s, e) =>
            {
                Console.WriteLine("MainWindow: Okno jest zamykane.");
                Application.Current.Shutdown();
            };

            this.Closed += (s, e) =>
            {
                Console.WriteLine("MainWindow: Okno zostało zamknięte.");
                Application.Current.Shutdown();
            };

            CurrentUser = user;
            _connectionString = connectionString;
            _connectionString = "Host = localhost; Port = 5432; Username = postgres; Password = postgres; Database = ids_system";

            AlertGroups = new ObservableCollection<AlertGroup>();

            LoadAlerts();

            DataContext = this;


            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += CheckForNewAlerts;
            _timer.Start();
        }

        private void LoadAlerts()
        {
            var allAlerts = AlertRepository.GetAlerts(_connectionString) ?? new List<Alert>();

            switch (CurrentUser.Role)
            {
                case "guest":
                    allAlerts = allAlerts.Where(a => a.DestinationIp == localIp).ToList();
                    break;

                case "user":
                    allAlerts = allAlerts.Where(a => a.DestinationIp == CurrentUser.AssignedIp).ToList();
                    break;

            }

            // Grupowanie alertów według DestinationIp
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
            var newAlerts = AlertRepository.GetAlerts(_connectionString)
                                           .Where(a => a.Id > _lastMaxId)
                                           .ToList();

            if (CurrentUser.Role == "guest")
            {
                //string localIp = GetLocalIpAddress();
                newAlerts = newAlerts
                    .Where(a => a.DestinationIp == localIp)
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



        private void RemoveEmptyGroups()
        {
            var emptyGroups = AlertGroups.Where(g => !g.Alerts.Any()).ToList();
            foreach (var group in emptyGroups)
            {
                AlertGroups.Remove(group);
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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow(_connectionString);
            if (loginWindow.ShowDialog() == true && loginWindow.LoggedUser != null)
            {
                CurrentUser = loginWindow.LoggedUser;
                MessageBox.Show($"Zalogowano jako: {CurrentUser.Username}", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"Zalogowano użytkownika: {CurrentUser.Username} ({CurrentUser.Role})");
                LoadAlerts();
            }
            else
            {
                Console.WriteLine("Logowanie nie powiodło się.");
            }
        }
        private static string GetLocalIpAddress()
        {
            string hostName = Dns.GetHostName(); // Pobierz nazwę hosta
            var addresses = Dns.GetHostAddresses(hostName); // Pobierz adresy IP

            // Znajdź pierwszy adres IPv4
            foreach (var address in addresses)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }

            throw new Exception("Nie znaleziono lokalnego adresu IPv4.");
        }
    }
}