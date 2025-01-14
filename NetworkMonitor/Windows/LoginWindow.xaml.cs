using System.Net.Http;
using System.Net;
using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;
using System.Net.Http.Json;

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


        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;


            using (var httpClient = new HttpClient())
            {
                var loginRequest = new { Username = username, Password = password };

                try
                {
                    var response = await httpClient.PostAsJsonAsync("http://localhost:5136/api/auth/login", loginRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var user = await response.Content.ReadFromJsonAsync<User>();
                        LoggedUser = user;
                        DialogResult = true; // Zamknij okno logowania
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        MessageBox.Show("Nieprawidłowa nazwa użytkownika lub hasło.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Wystąpił błąd podczas logowania.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd podczas komunikacji z serwerem: {ex.Message}", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
