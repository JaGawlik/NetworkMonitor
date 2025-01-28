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
            if (_isAdmin)
            {
                MessageBox.Show("Jesteś już zalogowany jako administrator.", "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var viewModel = DataContext as MainWindowViewModel;

            // Otwarcie okna logowania
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && loginWindow.LoggedUser != null)
            {
                // Pobranie zalogowanego użytkownika
                var loggedUser = loginWindow.LoggedUser;
                viewModel.CurrentUser = loggedUser;
                viewModel.IsAdminLoggedIn = true;


                // Wyświetlenie komunikatu
                MessageBox.Show($"Zalogowano jako: {viewModel.CurrentUser.Username}",
                    "Logowanie", MessageBoxButton.OK, MessageBoxImage.Information);

                Console.WriteLine($"Zalogowano użytkownika: {viewModel.CurrentUser.Username} ({viewModel.CurrentUser.Role})");

                // Załaduj alerty zgodnie z nową rolą użytkownika
                viewModel.AlertGroupViewModels.Clear();
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

            // Ustawienie użytkownika jako niezalogowanego
            viewModel.CurrentUser = new User { Role = "Guest", Username = "Niezalogowany" };
            viewModel.IsAdminLoggedIn = false;
            viewModel.SelectedTabIndex = 0;

            // Wyświetlenie komunikatu
            MessageBox.Show("Wylogowano. Widoczne będą tylko alerty związane z lokalnym adresem IP.",
                "Wylogowanie", MessageBoxButton.OK, MessageBoxImage.Information);

            Console.WriteLine("Wylogowano. Rola: Guest");
            // Wyczyść alerty i załaduj tylko te przypisane do lokalnego IP
            viewModel.AlertGroupViewModels.Clear();
            viewModel.LoadAlerts();
        }


    }
}
