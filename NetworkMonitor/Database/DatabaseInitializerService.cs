//using NetworkMonitor.Configuration;
//using NetworkMonitor.Database;
//using NetworkMonitor.Repository;
//using System.Windows;

//namespace NetworkMonitor.Database
//{
//    public class DatabaseInitializerService
//    {
//        /// <summary>
//        /// Inicjalizuje bazę danych.
//        /// </summary>
//        public void InitializeDatabase()
//        {
//            try
//            {
//                // Pobranie connection stringa z pliku config.json
//                string connectionString = ConfigurationManager.GetSetting("ConnectionString");
//                string targetDatabase = "ids_system"; // Możesz uczynić to dynamicznym, jeśli konieczne

//                // Sprawdzenie lub utworzenie bazy danych i tabel
//                DatabaseInit.EnsureDatabaseExists(connectionString, targetDatabase);

//                // Zapisanie finalnego connection stringa w config.json
//                string finalConnectionString = connectionString.Replace("Database=postgres", $"Database={targetDatabase}");
//                ConfigurationManager.SetSetting("ConnectionString", finalConnectionString);
//                ConfigurationManager.SaveSettings();

//                MessageBox.Show("Baza danych została zainicjalizowana.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Błąd podczas inicjalizacji bazy danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
//                Application.Current.Shutdown();
//            }
//        }

//        /// <summary>
//        /// Sprawdza, czy w bazie danych istnieją użytkownicy, i w razie potrzeby uruchamia okno dodawania użytkownika.
//        /// </summary>
//        public bool EnsureUsersExist()
//        {
//            try
//            {
//                string connectionString = ConfigurationManager.GetSetting("ConnectionString");

//                if (!UserRepository.HasUsers(connectionString))
//                {
//                    var addUserWindow = new AddUserWindow(connectionString);
//                    return addUserWindow.ShowDialog() == true;
//                }

//                return true;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Błąd podczas sprawdzania użytkowników: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
//                return false;
//            }
//        }
//    }
   
//}
