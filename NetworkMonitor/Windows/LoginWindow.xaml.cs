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
        public User LoggedUser { get; private set; }
        public LoginWindow()
        {
            InitializeComponent();
        }


        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Nazwa użytkownika i hasło nie mogą być puste.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var httpClient = new HttpClient())
            {
                var loginRequest = new { Username = username, Password = password };

                try
                {
                    var response = await httpClient.PostAsJsonAsync("http://localhost:5136/api/auth/login", loginRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        // Pobranie danych użytkownika z odpowiedzi API
                        var user = await response.Content.ReadFromJsonAsync<User>();
                        if (user != null)
                        {
                            LoggedUser = user;
                            DialogResult = true; // Zamknij okno logowania i przekaż sukces
                        }
                        else
                        {
                            MessageBox.Show("Nie udało się pobrać danych użytkownika.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        MessageBox.Show("Nieprawidłowa nazwa użytkownika lub hasło.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Wystąpił błąd podczas logowania: {response.StatusCode}.", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
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
