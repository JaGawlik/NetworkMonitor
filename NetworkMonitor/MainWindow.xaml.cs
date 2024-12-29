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

            if (CurrentUser.Role == "user")
            {
                // Filtruj alerty dla zwykłego użytkownika
                allAlerts = allAlerts
                    .Where(a => a.DestinationIp == CurrentUser.AssignedIp)
                    .ToList();
            }

            // Grupowanie alertów według DestinationIp
            var groupedAlerts = allAlerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new AlertGroup
                {
                    DestinationIp = group.Key,
                    Alerts = group.ToList()
                })
                .ToList();

            // Przypisz grupy alertów do AlertGroups
            AlertGroups.Clear();
            foreach (var group in groupedAlerts)
            {
                AlertGroups.Add(group);
            }

            //Console.WriteLine($"Załadowano {AlertGroups.Count} grup alertów.");
            AlertGroups.Clear();

            AlertGroups.Add(new AlertGroup
            {
                DestinationIp = "192.168.1.1",
                Alerts = new List<Alert>
        {
            new Alert { Id = 1, Timestamp = DateTime.Now, AlertMessage = "Test Alert 1" },
            new Alert { Id = 2, Timestamp = DateTime.Now, AlertMessage = "Test Alert 2" }
        }
            });

            Console.WriteLine("Dane testowe zostały załadowane.");
        }


        private void CheckForNewAlerts(object sender, EventArgs e)
        {
            var newAlerts = AlertRepository.GetAlerts(_connectionString)
                                           .Where(a => a.Id > _lastMaxId)
                                           .ToList();

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


    }
}