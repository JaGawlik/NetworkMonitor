using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using NetworkMonitor.Model;

namespace AlertApiServer.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        [HttpPost]
        public IActionResult ReceiveAlert([FromBody] Alert alert)
        {
            try
            {
                string connectionString = "Host=localhost;Username=postgres;Password=postgres;Database=ids_system";
                using var conn = new NpgsqlConnection(connectionString);
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
    }
}