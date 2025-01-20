using NetworkMonitor.Configuration;
using NetworkMonitor.Repository;
using NetworkMonitor.Windows;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var dbConfigWindow = new DatabaseConfigWindow();

            try
            {
                // Pobranie connection stringa z pliku config.json
                string connectionString = ConfigurationManager.GetSetting("ConnectionString");

                // Sprawdzenie, czy connection string jest pusty
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show("Connection string jest pusty. Konieczna konfiguracja bazy danych.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Wyświetlenie okna konfiguracji bazy danych
                    
                    bool? result = dbConfigWindow.ShowDialog();

                    if (result == true)
                    {
                        // Generowanie connection stringa na podstawie danych wprowadzonych przez użytkownika
                        connectionString = GenerateConnectionString(
                            dbConfigWindow.Host,
                            dbConfigWindow.Port,
                            dbConfigWindow.Username,
                            dbConfigWindow.Password,
                            dbConfigWindow.SQLDatabaseName
                        );

                        // Zapis connection stringa do pliku config.json
                        ConfigurationManager.SetSetting("ConnectionString", connectionString);
                        ConfigurationManager.SaveSettings();
                    }
                    else
                    {
                        MessageBox.Show("Konfiguracja bazy danych została anulowana. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return;
                    }
                }

                // Tworzenie bazy danych, jeśli nie istnieje
                EnsureDatabaseExists(connectionString,dbConfigWindow.DatabaseName);

                // Zapisanie finalnego connection stringa w config.json
                string finalConnectionString = connectionString.Replace("Database=postgres", $"Database={dbConfigWindow.DatabaseName}");
                ConfigurationManager.SetSetting("ConnectionString", finalConnectionString);
                ConfigurationManager.SaveSettings();

                MessageBox.Show("Baza danych została zainicjalizowana.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // Połączenie z bazą "postgres", aby stworzyć nową bazę danych
                string baseConnectionString = connectionString.Replace($"Database={dbName}", "Database=postgres");
                Console.WriteLine($"Connection string dla bazy 'postgres': {baseConnectionString}");

                using var conn = new NpgsqlConnection(baseConnectionString);
                conn.Open();

                // Sprawdź, czy baza danych istnieje
                using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", conn))
                {
                    cmd.Parameters.AddWithValue("dbName", dbName);
                    var exists = cmd.ExecuteScalar();
                    if (exists == null)
                    {
                        // Tworzenie nowej bazy danych
                        Console.WriteLine($"Tworzenie bazy danych: {dbName}");
                        using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine($"Baza danych '{dbName}' została utworzona.");
                    }
                    else
                    {
                        Console.WriteLine($"Baza danych '{dbName}' już istnieje.");
                    }
                }

                // Tworzenie tabel w docelowej bazie danych
                string targetConnectionString = connectionString.Replace("Database=postgres", $"Database={dbName}");
                Console.WriteLine($"Connection string dla docelowej bazy danych: {targetConnectionString}");
                CreateTables(targetConnectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas sprawdzania lub tworzenia bazy danych: {ex.Message}");
                throw;
            }
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
                    );
                ";

                string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password VARCHAR(255) NOT NULL,
                        role VARCHAR(20) DEFAULT 'user',
                        assigned_ip VARCHAR(50)
                    );
                ";

                using (var cmd = new NpgsqlCommand(createAlertsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand(createUsersTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine("Tabele zostały utworzone lub już istniały.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas tworzenia tabel: {ex.Message}");
                MessageBox.Show($"Błąd podczas tworzenia tabel: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// Generuje dynamiczny connection string na podstawie parametrów.
        /// </summary>
        public static string GenerateConnectionString(string host, int port, string username, string password, string database)
        {
            return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
        }
    }
}