using NetworkMonitor.AppConfiguration;
using NetworkMonitor.Model;
using NetworkMonitor.Repository;
using NetworkMonitor.Windows;
using Npgsql;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace NetworkMonitor.Database
{
    public class DatabaseService
    {
        /// <summary>
        /// Inicjalizuje bazę danych, tworząc ją, jeśli nie istnieje, i dodając odpowiednie tabele.
        /// </summary>
        /// 

        public void InitializeDatabase()
        {
            try
            {
                string connectionString = GetConnectionStringForInitialization();

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("Konfiguracja bazy danych została anulowana. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }

                string databaseName = GetDatabaseNameFromConnectionString(connectionString);

                EnsureDatabaseExists(connectionString, databaseName);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji bazy danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Sprawdza, czy w bazie danych istnieją użytkownicy, i w razie potrzeby uruchamia okno dodawania użytkownika.
        /// </summary>
        /// 
        private string GetConnectionStringForInitialization()
        {
            var dbConfigWindow = new DatabaseConfigWindow();
            if (dbConfigWindow.ShowDialog() == true)
            {
                return GenerateConnectionString(
                    dbConfigWindow.Host,
                    dbConfigWindow.Port,
                    dbConfigWindow.Username,
                    dbConfigWindow.Password,
                    dbConfigWindow.DatabaseNameTextBox.Text
                );
            }

            throw new Exception("Nie podano konfiguracji bazy danych.");
        }
        public async Task<bool> EnsureUsersExistAsync()
        {
            try
            {
                if (ConfigurationManager.Settings.Role == "Administrator")
                {
                    //MessageBox.Show("Wykrywanie API dla administratora nastąpi po zakończeniu inicjalizacji bazy danych.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await ConfigurationManager.InitializeApiUrlAsync(); 
                }

                if (string.IsNullOrWhiteSpace(ConfigurationManager.Settings.ApiUrl))
                {
                    string detectedApiUrl = await ConfigurationManager.DiscoverApiAddressAsync();
                    ConfigurationManager.Settings.ApiUrl = detectedApiUrl;
                    ConfigurationManager.SaveSettings();
                }

                using var client = new HttpClient { BaseAddress = new Uri(ConfigurationManager.Settings.ApiUrl) };
                var response = await client.GetAsync("/api/users");

                // Jeśli nie można pobrać użytkowników, rzucamy wyjątek
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Błąd API: {response.ReasonPhrase}");
                }

                var users = await response.Content.ReadFromJsonAsync<List<User>>();

                // Jeśli lista użytkowników jest pusta, wyświetlamy okno dodawania administratora
                if (users == null || !users.Any())
                {
                    MessageBox.Show("Brak zarejestrowanych użytkowników. Dodaj pierwszego administratora.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);

                    var addUserWindow = new AddUserWindow();
                    return addUserWindow.ShowDialog() == true;
                }

                // Jeśli użytkownicy istnieją, zwracamy true
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas sprawdzania użytkowników: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    


        /// <summary>
        /// Tworzy bazę danych, jeśli nie istnieje, oraz tabele w niej.
        /// </summary>
        private void EnsureDatabaseExists(string connectionString, string dbName)
        {
            try
            {
                if (!DatabaseExists(connectionString, dbName))
                {
                    CreateDatabase(connectionString, dbName);
                    string targetConnectionString = connectionString.Replace("Database=postgres", $"Database={dbName}");
                    CreateTables(targetConnectionString);

                    MessageBox.Show("Baza danych została zainicjalizowana.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas sprawdzania lub tworzenia bazy danych: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sprawdza, czy baza danych istnieje.
        /// </summary>
        private bool DatabaseExists(string connectionString, string dbName)
        {
            string baseConnectionString = connectionString.Replace($"Database={dbName}", "Database=postgres");

            using var conn = new NpgsqlConnection(baseConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", conn);
            cmd.Parameters.AddWithValue("dbName", dbName);
            return cmd.ExecuteScalar() != null;
        }

        /// <summary>
        /// Tworzy nową bazę danych.
        /// </summary>
        private void CreateDatabase(string connectionString, string dbName)
        {
            string baseConnectionString = connectionString.Replace($"Database={dbName}", "Database=postgres");

            using var conn = new NpgsqlConnection(baseConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Tworzy tabele w bazie danych, jeśli nie istnieją.
        /// </summary>
        private void CreateTables(string connectionString)
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string createAlertsTable = @"
                    CREATE TABLE IF NOT EXISTS alerts (
                        id SERIAL PRIMARY KEY,
                        timestamp TIMESTAMP NOT NULL,
                        alert_message TEXT NOT NULL,
                        source_ip VARCHAR(45),
                        destination_ip VARCHAR(45),
                        protocol VARCHAR(10),
                        status VARCHAR(20) DEFAULT 'new',
                        snort_instance VARCHAR(100),
                        signature_id INT NOT NULL DEFAULT 0
                    );";

                string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password VARCHAR(255) NOT NULL,
                        role VARCHAR(20) DEFAULT 'user',
                        assigned_ip VARCHAR(50)
                    );";

                ExecuteNonQuery(conn, createAlertsTable);
                ExecuteNonQuery(conn, createUsersTable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas tworzenia tabel: {ex.Message}");
                MessageBox.Show($"Błąd podczas tworzenia tabel: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Wykonuje zapytanie SQL bez zwracania wyników.
        /// </summary>
        private void ExecuteNonQuery(NpgsqlConnection conn, string query)
        {
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Pobiera lub tworzy connection string.
        /// </summary>
        private string GetOrCreateConnectionString()
        {
            string connectionString = ConfigurationManager.GetSetting("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var dbConfigWindow = new DatabaseConfigWindow();
                if (dbConfigWindow.ShowDialog() == true)
                {
                    connectionString = GenerateConnectionString(
                        dbConfigWindow.Host,
                        dbConfigWindow.Port,
                        dbConfigWindow.Username,
                        dbConfigWindow.Password,
                        dbConfigWindow.DatabaseNameTextBox.Text
                    );

                }
            }

            return connectionString;
        }

        /// <summary>
        /// Generuje dynamiczny connection string na podstawie parametrów.
        /// </summary>
        public static string GenerateConnectionString(string host, int port, string username, string password, string database)
        {
            return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
        }

        /// <summary>
        /// Pobiera nazwę bazy danych z connection stringa.
        /// </summary>
        private string GetDatabaseNameFromConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.Database;
        }
    }
}
