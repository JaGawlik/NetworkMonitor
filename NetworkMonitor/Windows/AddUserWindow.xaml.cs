using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Database;
using NetworkMonitor.Repository;

namespace NetworkMonitor
{
    public partial class AddUserWindow : Window
    {
        private readonly string _connectionString;

        public AddUserWindow(string connectionString)
        {
            InitializeComponent();
            _connectionString = connectionString;
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Wszystkie pola są wymagane.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            var adminUser = new User
            {
                Username = username,
                Password = password, 
                Role = "Administrator",
                AssignedIp = null 
            };

            UserRepository.AddUser(_connectionString, adminUser);

            MessageBox.Show("Administrator został dodany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
    }
}
