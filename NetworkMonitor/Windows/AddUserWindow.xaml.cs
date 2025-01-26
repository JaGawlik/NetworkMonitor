using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.AppConfiguration;
using NetworkMonitor.Model;

namespace NetworkMonitor
{
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
        }

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Wszystkie pola są wymagane.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (password.Length < 8)
            {
                ErrorText.Text = "Hasło musi mieć co najmniej 8 znaków.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            var addUserRequest = new AddUserRequest
            {
                Username = username,
                Password = password,
                Role = "Administrator",
                AssignedIp = null
            };

            try
            {
                bool success = await AddUserViaApi(addUserRequest);
                if (success)
                {
                    MessageBox.Show("Administrator został dodany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    ErrorText.Text = "Nie udało się dodać użytkownika. Spróbuj ponownie.";
                    ErrorText.Visibility = Visibility.Visible;
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Błąd podczas dodawania użytkownika: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> AddUserViaApi(AddUserRequest userRequest)
        {
            using var client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.Settings.ApiUrl) };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(userRequest, options);
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/users/add", httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Błąd API: {response.ReasonPhrase}");
                Console.WriteLine($"Szczegóły błędu: {errorDetails}");
                throw new Exception($"Błąd API: {response.ReasonPhrase}");
            }

            return response.IsSuccessStatusCode;
        }

        public class AddUserRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public string? AssignedIp { get; set; }
        }
    }
}
