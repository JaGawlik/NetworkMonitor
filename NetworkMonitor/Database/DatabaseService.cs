using NetworkMonitor.Configuration;
using NetworkMonitor.Repository;
using NetworkMonitor.Windows;
using Npgsql;
using System;
using System.Windows;

namespace NetworkMonitor.Database
{
    public class DatabaseService
    {
        /// <summary>
        /// Inicjalizuje bazę danych, tworząc ją, jeśli nie istnieje, i dodając odpowiednie tabele.
        /// </summary>
        public void InitializeDatabase()
        {
            try
            {
                string connectionString = GetOrCreateConnectionString();

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("Konfiguracja bazy danych została anulowana. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }

                // Tworzenie bazy danych, jeśli nie istnieje
                EnsureDatabaseExists(connectionString, GetDatabaseNameFromConnectionString(connectionString));

                
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
        public bool EnsureUsersExist()
        {
            try
            {
                string connectionString = ConfigurationManager.GetSetting("ConnectionString");

                if (!UserRepository.HasUsers(connectionString))
                {
                    var addUserWindow = new AddUserWindow(connectionString);
                    return addUserWindow.ShowDialog() == true;
                }

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
                        snort_instance VARCHAR(100)
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

                    ConfigurationManager.SetSetting("ConnectionString", connectionString);
                    ConfigurationManager.SaveSettings();
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
