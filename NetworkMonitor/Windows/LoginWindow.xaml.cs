using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;

namespace NetworkMonitor
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString;
        //private readonly string _connectionString = ((App)Application.Current).DBConnectionString;
        public User LoggedUser { get; private set; }
        public LoginWindow(string connectionString)
        {
            InitializeComponent();
            _connectionString = connectionString;
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;


            LoggedUser = UserRepository.Authenticate(_connectionString, username, password);
            if (LoggedUser != null)
            {
                DialogResult = true; // Zamknij okno logowania i kontynuuj
            }
            else
            {
                MessageBox.Show("Nieprawidłowa nazwa użytkownika lub hasło.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
