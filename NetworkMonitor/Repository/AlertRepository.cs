using NetworkMonitor.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Repository
{
    internal class AlertRepository
    {
        private readonly string _apiUrl;

        public AlertRepository(string apiUrl)
        {
            _apiUrl = apiUrl;
        }
        public async Task<List<Alert>> GetAlertsAsync(string ip = null, string assignedIp = null)
        {
            using var httpClient = new HttpClient();
            string url = $"{_apiUrl}/api/alerts";

            if (!string.IsNullOrEmpty(ip))
                url += $"?ip={ip}";
            else if (!string.IsNullOrEmpty(assignedIp))
                url += $"?assignedIp={assignedIp}";

            var response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Alert>>();
            }

            throw new Exception($"Błąd podczas pobierania alertów: {response.StatusCode}");

        }

        public async Task UpdateAlertStatusAsync(int alertId, string newStatus)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.PutAsJsonAsync($"{_apiUrl}/api/alerts/{alertId}/status", newStatus);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Błąd podczas aktualizacji statusu alertu: {response.StatusCode}");
            }
        }
    }
}
