using System;
using System.Windows;
using NetworkMonitor.Model;
using NetworkMonitor.Windows;

namespace NetworkMonitor
{
    public partial class MainWindow : Window
    {
        private readonly bool _isAdmin;
        public MainWindow(User user)
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

            DataContext = new MainWindowViewModel(user);;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAdmin) {
                MessageBox.Show("Jesteś już zalogowany jako administrator.", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var viewModel = DataContext as MainWindowViewModel;

            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && loginWindow.LoggedUser != null)
            {
                var loggedUser = loginWindow.LoggedUser;
                viewModel.CurrentUser = loggedUser;

                MessageBox.Show($"Zalogowano jako: {viewModel.CurrentUser.Username}", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"Zalogowano użytkownika: {viewModel.CurrentUser.Username} ({viewModel.CurrentUser.Role})");

                viewModel.LoadAlerts();
            }
            else
            {
                Console.WriteLine("Okno logowania zostało zamknięte.");
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel.CurrentUser = new User { Role = "User", Username = "Niezalogowany" };
            MessageBox.Show("Wylogowano.", "Wylogowanie", MessageBoxButton.OK, MessageBoxImage.Information);
            Console.WriteLine("Wylogowano. Rola: guest");
            viewModel.LoadAlerts();
        }

        private void ConfigureUIForRole()
        {
            if (!_isAdmin)
            {
                // Ukryj przyciski administracyjne dla klienta
                //AdminPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
