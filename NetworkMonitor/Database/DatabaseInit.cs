using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Database
{
    internal class DatabaseInit
    {
        public static void EnsureDatabaseExists(string connectionString, string dbName)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", conn))
            {
                cmd.Parameters.AddWithValue("dbName", dbName);
                var exists = cmd.ExecuteScalar();
                if (exists == null)
                {
                    using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
                    createCmd.ExecuteNonQuery();
                    Console.WriteLine($"Baza danych '{dbName}' została utworzona.");
                }
                else
                {
                    Console.WriteLine($"Baza danych '{dbName}' już istnieje.");
                }
            }
        }

        public static void CreateTables(string connectionString)
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
                    password_hash VARCHAR(255) NOT NULL,
                    role VARCHAR(20) DEFAULT 'user'
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
    }
}
