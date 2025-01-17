﻿using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using NetworkMonitor.Model;

namespace AlertApiServer.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private const string ConnectionString = "Host=localhost;Username=postgres;Password=postgres;Database=ids_system";

        [HttpPost]
        public IActionResult ReceiveAlert([FromBody] Alert alert)
        {
            Console.WriteLine($"Otrzymano alert: {alert.AlertMessage} od {alert.SourceIp} do {alert.DestinationIp}");
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string query = @"
                INSERT INTO alerts (timestamp, alert_message, source_ip, destination_ip, protocol, status, snort_instance)
                VALUES (@timestamp, @alertMessage, @sourceIp, @destinationIp, @protocol, @status, @snortInstance)";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("timestamp", alert.Timestamp);
                cmd.Parameters.AddWithValue("alertMessage", alert.AlertMessage);
                cmd.Parameters.AddWithValue("sourceIp", alert.SourceIp);
                cmd.Parameters.AddWithValue("destinationIp", alert.DestinationIp);
                cmd.Parameters.AddWithValue("protocol", alert.Protocol);
                cmd.Parameters.AddWithValue("status", alert.Status);
                cmd.Parameters.AddWithValue("snortInstance", alert.SnortInstance);
                cmd.ExecuteNonQuery();

                return Ok("Alert received and stored.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving alert: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetAlerts(string? ip = null, string? role = null, string? assignedIp = null)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                // Podstawowe zapytanie
                string query = "SELECT * FROM alerts";

                // Filtruj alerty na podstawie parametrów
                List<string> conditions = new List<string>();
                if (!string.IsNullOrEmpty(ip))
                {
                    conditions.Add("destination_ip = @ip");
                }
                if (!string.IsNullOrEmpty(assignedIp))
                {
                    conditions.Add("destination_ip = @assignedIp");
                }

                if (conditions.Any())
                {
                    query += " WHERE " + string.Join(" OR ", conditions);
                }

                query += " ORDER BY timestamp DESC";

                using var cmd = new NpgsqlCommand(query, conn);

                if (!string.IsNullOrEmpty(ip))
                {
                    cmd.Parameters.AddWithValue("ip", ip);
                }
                if (!string.IsNullOrEmpty(assignedIp))
                {
                    cmd.Parameters.AddWithValue("assignedIp", assignedIp);
                }

                using var reader = cmd.ExecuteReader();
                var alerts = new List<Alert>();

                while (reader.Read())
                {
                    alerts.Add(new Alert
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                        AlertMessage = reader.GetString(reader.GetOrdinal("alert_message")),
                        SourceIp = reader.GetString(reader.GetOrdinal("source_ip")),
                        DestinationIp = reader.GetString(reader.GetOrdinal("destination_ip")),
                        Protocol = reader.GetString(reader.GetOrdinal("protocol")),
                        Status = reader.GetString(reader.GetOrdinal("status")),
                        SnortInstance = reader.GetString(reader.GetOrdinal("snort_instance"))
                    });
                }

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching alerts: {ex.Message}");
                return StatusCode(500, "Error fetching alerts.");
            }
        }

        [HttpPut("{id}/status")]
        public IActionResult UpdateAlertStatus(int id, [FromBody] string newStatus)
        {
            try
            {
                string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=ids_system";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                string query = @"
                    UPDATE alerts
                    SET status = @newStatus
                    WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("newStatus", newStatus);
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok("Alert status updated successfully.");
                }
                else
                {
                    return NotFound("Alert not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating alert status: {ex.Message}");
            }
        }
    }
}