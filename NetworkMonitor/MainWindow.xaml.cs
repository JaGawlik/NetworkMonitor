using NetworkMonitor.Database;
using NetworkMonitor.Model;
using System.Collections.ObjectModel;
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
    public partial class MainWindow : Window
    {
        public ObservableCollection<Alert> Alerts { get; set; }
        private string _connectionString;
        private DispatcherTimer _timer;
        private int _lastMaxId = 0;

        private User CurrentUser { get; set; }

        public ObservableCollection<AlertGroup> AlertGroups { get; set; }
        public MainWindow(User user, string connectionString)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null.");
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

            InitializeComponent();

            CurrentUser = user;
            _connectionString = connectionString;

           
            AlertGroups = new ObservableCollection<AlertGroup>();

            DataContext = this;

            LoadAlerts();
                   

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += CheckForNewAlerts;
            _timer.Start();
        }

        private void LoadAlerts()
        {
            var allAlerts = AlertRepository.GetAlerts(_connectionString);

            if (CurrentUser.Role == "user")
            {
                // Filtrowanie dla zwykłych użytkowników
                allAlerts = allAlerts
                    .Where(a => a.DestinationIp == CurrentUser.AssignedIp)
                    .ToList();
            }

            var groupedAlerts = allAlerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new AlertGroup
                {
                    DestinationIp = group.Key,
                    Alerts = group.ToList()
                });

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

            if (newAlerts.Any())
            {
                Console.WriteLine($"Znaleziono {newAlerts.Count} nowych alertów.");
                _lastMaxId = newAlerts.Max(a => a.Id);

                foreach (var alert in newAlerts)
                {
                    Console.WriteLine($"Nowy alert: {alert.AlertMessage} do {alert.DestinationIp}");
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

                RemoveEmptyGroups();
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

    }
}