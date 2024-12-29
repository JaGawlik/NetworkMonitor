using NetworkMonitor.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Database
{
    internal class AlertRepository
    {       
        public static List<Alert> GetAlerts(string connectionString)
        {
            Console.WriteLine("Pobieranie alertów z bazy danych...");
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT id, timestamp, alert_message, source_ip, destination_ip, protocol, status
                FROM alerts
                ORDER BY timestamp DESC";

            using var command = new NpgsqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            var alerts = new List<Alert>();
            while (reader.Read())
            {
                alerts.Add(new Alert
                {
                    Id = reader.GetInt32(0),
                    Timestamp = reader.GetDateTime(1),
                    AlertMessage = reader.GetString(2),
                    SourceIp = reader.GetString(3),
                    DestinationIp = reader.GetString(4),
                    Protocol = reader.GetString(5),
                    Status = reader.GetString(6)
                });
            }

            Console.WriteLine($"Pobrano {alerts.Count} alertów.");
            return alerts;
        }

    }
}
