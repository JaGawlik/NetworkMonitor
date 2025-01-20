//using Npgsql;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using NetworkMonitor.Configuration;

//namespace NetworkMonitor.Database
//{
//    internal class DatabaseInit
//    {
//        public static void EnsureDatabaseExists(string connectionString, string dbName)
//        {
//            try
//            {
//                using var conn = new NpgsqlConnection(connectionString);
//                conn.Open();

//                // Sprawdź, czy baza danych istnieje
//                using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", conn))
//                {
//                    cmd.Parameters.AddWithValue("dbName", dbName);
//                    var exists = cmd.ExecuteScalar();
//                    if (exists == null)
//                    {
//                        // Tworzenie nowej bazy danych
//                        using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
//                        createCmd.ExecuteNonQuery();
//                        Console.WriteLine($"Baza danych '{dbName}' została utworzona.");
//                    }
//                    else
//                    {
//                        Console.WriteLine($"Baza danych '{dbName}' już istnieje.");
//                    }
//                }

//                // Tworzenie tabel w docelowej bazie danych
//                string targetConnectionString = connectionString.Replace("Database=postgres", $"Database={dbName}");
//                CreateTables(targetConnectionString);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Błąd podczas sprawdzania lub tworzenia bazy danych: {ex.Message}");
//                throw;
//            }
//        }

//        public static void CreateTables(string connectionString)
//        {
//            try
//            {
//                using var conn = new NpgsqlConnection(connectionString);
//                conn.Open();

//                string createAlertsTable = @"
//                    CREATE TABLE IF NOT EXISTS alerts (
//                        id SERIAL PRIMARY KEY,
//                        timestamp TIMESTAMP NOT NULL,
//                        alert_message TEXT NOT NULL,
//                        source_ip VARCHAR(45),
//                        destination_ip VARCHAR(45),
//                        protocol VARCHAR(10),
//                        status VARCHAR(20) DEFAULT 'new',
//                        snort_instance VARCHAR(100)
//                    );
//                ";

//                string createUsersTable = @"
//                    CREATE TABLE IF NOT EXISTS users (
//                        id SERIAL PRIMARY KEY,
//                        username VARCHAR(50) UNIQUE NOT NULL,
//                        password VARCHAR(255) NOT NULL,
//                        role VARCHAR(20) DEFAULT 'user',
//                        assigned_ip VARCHAR(50)
//                    );
//                ";

//                using (var cmd = new NpgsqlCommand(createAlertsTable, conn))
//                {
//                    cmd.ExecuteNonQuery();
//                }

//                using (var cmd = new NpgsqlCommand(createUsersTable, conn))
//                {
//                    cmd.ExecuteNonQuery();
//                }

//                Console.WriteLine("Tabele zostały utworzone lub już istniały.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Błąd podczas tworzenia tabel: {ex.Message}");
//                throw;
//            }
//        }

//        public static string GenerateConnectionString(string host, int port, string username, string password, string database)
//        {
//            return $"Host={host};Port={port};Username={username};Password={password};Database={database}";
//        }

        

//    }
//}
