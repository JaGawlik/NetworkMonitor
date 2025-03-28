﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMonitor.Model;

namespace AlertApiServer.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private readonly string _connectionString;

        public AlertsController(IConfiguration configuration)
        {
            // Pobierz connection string z konfiguracji
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost]
        public IActionResult ReceiveAlert([FromBody] Alert alert)
        {
            Console.WriteLine($"Otrzymano alert: {alert.AlertMessage} od {alert.SourceIp} do {alert.DestinationIp}");
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string query = @"
                INSERT INTO alerts (timestamp, alert_message, source_ip, source_port, destination_ip, destination_port, protocol, status, snort_instance, signature_id)
                VALUES (@timestamp, @alertMessage, @sourceIp, @sourcePort, @destinationIp, @destinationPort, @protocol, @status, @snortInstance, @signatureId)";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("timestamp", alert.Timestamp);
                cmd.Parameters.AddWithValue("alertMessage", alert.AlertMessage);
                cmd.Parameters.AddWithValue("sourceIp", alert.SourceIp);
                cmd.Parameters.AddWithValue("sourcePort", alert.SourcePort ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("destinationIp", alert.DestinationIp);
                cmd.Parameters.AddWithValue("destinationPort", alert.DestinationPort ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("protocol", alert.Protocol);
                cmd.Parameters.AddWithValue("status", alert.Status);
                cmd.Parameters.AddWithValue("snortInstance", alert.SnortInstance);
                cmd.Parameters.AddWithValue("signatureId", alert.SignatureId);
                cmd.ExecuteNonQuery();

                return Ok("Alert received and stored.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving alert: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetAlerts(string? ip = null, string? role = null, string? assignedIp = null, string? status = "new")
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT * FROM alerts";
                List<string> conditions = new List<string>();

                if (!string.IsNullOrEmpty(ip))
                {
                    conditions.Add("destination_ip = @ip");
                }
                if (!string.IsNullOrEmpty(assignedIp))
                {
                    conditions.Add("destination_ip = @assignedIp");
                }
                if (!string.IsNullOrEmpty(status))
                {
                    conditions.Add("status = @status");
                }

                if (conditions.Any())
                {
                    query += " WHERE " + string.Join(" AND ", conditions);
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
                if (!string.IsNullOrEmpty(status))
                {
                    cmd.Parameters.AddWithValue("status", status);
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
                        SourcePort = reader.IsDBNull(reader.GetOrdinal("source_port")) ? null : reader.GetInt32(reader.GetOrdinal("source_port")),
                        DestinationIp = reader.GetString(reader.GetOrdinal("destination_ip")),
                        DestinationPort = reader.IsDBNull(reader.GetOrdinal("destination_port")) ? null : reader.GetInt32(reader.GetOrdinal("destination_port")),
                        Protocol = reader.GetString(reader.GetOrdinal("protocol")),
                        Status = reader.GetString(reader.GetOrdinal("status")),
                        SnortInstance = reader.GetString(reader.GetOrdinal("snort_instance")),
                        SignatureId = reader.GetInt32(reader.GetOrdinal("signature_id"))
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
                using var conn = new NpgsqlConnection(_connectionString);
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

        [HttpPost("batch")]
        public IActionResult ReceiveAlertsBatch([FromBody] List<Alert> alerts)
        {
            if (alerts == null || !alerts.Any())
            {
                return BadRequest("No alerts provided.");
            }

            Console.WriteLine($"Otrzymano {alerts.Count} alertów do przetworzenia.");

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                // Rozpocznij transakcję
                using var transaction = conn.BeginTransaction();

                try
                {
                    string query = @"
                INSERT INTO alerts (timestamp, alert_message, source_ip, source_port, destination_ip, destination_port, protocol, status, snort_instance, signature_id)
                VALUES (@timestamp, @alertMessage, @sourceIp, @sourcePort, @destinationIp, @destinationPort, @protocol, @status, @snortInstance, @signatureId)";

                    foreach (var alert in alerts)
                    {
                        using var cmd = new NpgsqlCommand(query, conn, transaction);
                        cmd.Parameters.AddWithValue("timestamp", alert.Timestamp);
                        cmd.Parameters.AddWithValue("alertMessage", alert.AlertMessage);
                        cmd.Parameters.AddWithValue("sourceIp", alert.SourceIp);
                        cmd.Parameters.AddWithValue("sourcePort", alert.SourcePort ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("destinationIp", alert.DestinationIp);
                        cmd.Parameters.AddWithValue("destinationPort", alert.DestinationPort ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("protocol", alert.Protocol);
                        cmd.Parameters.AddWithValue("status", alert.Status);
                        cmd.Parameters.AddWithValue("snortInstance", alert.SnortInstance);
                        cmd.Parameters.AddWithValue("signatureId", alert.SignatureId);
                        cmd.ExecuteNonQuery();
                    }

                    // Zatwierdź transakcję
                    transaction.Commit();

                    Console.WriteLine($"Pomyślnie zapisano {alerts.Count} alertów.");
                    return Ok($"{alerts.Count} alerts received and stored.");
                }
                catch (Exception ex)
                {
                    // Wycofaj transakcję w przypadku błędu
                    transaction.Rollback();
                    Console.WriteLine($"Błąd podczas zapisywania alertów: {ex.Message}");
                    return StatusCode(500, $"Error saving alerts: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd połączenia z bazą danych: {ex.Message}");
                return StatusCode(500, $"Database connection error: {ex.Message}");
            }
        }
    }
}
