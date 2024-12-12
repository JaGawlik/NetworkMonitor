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

        public ObservableCollection<AlertGroup> AlertGroups { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            _connectionString = ((App)Application.Current).DBConnectionString;

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

            if (allAlerts.Any())
            {
                _lastMaxId = allAlerts.Max(a => a.Id);
            }

            var groupedAlerts = GroupAlertsByDestinationIp(allAlerts);

            AlertGroups.Clear();
            foreach (var group in groupedAlerts)
            {
                AlertGroups.Add(group);
            }

            //SortAlerts();
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
                        existingGroup.Alerts.Add(alert);
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
            //SortAlerts();
            
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
        private void SortAlerts()
        {
            var sortedAlerts = Alerts.OrderByDescending(a => a.Timestamp).ToList();

            Alerts.Clear();
            foreach (var alert in sortedAlerts)
            {
                Alerts.Add(alert);
            }
        }

        private List<AlertGroup> GroupAlertsByDestinationIp(List<Alert> alerts)
        {
            return alerts
                .GroupBy(a => a.DestinationIp)
                .Select(group => new AlertGroup
                {
                    DestinationIp = group.Key,
                    Alerts = group.ToList()
                })
                .ToList();
        }
    }
}