using NetworkMonitor.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Repository
{    public static class UserRepository
    {
        public static bool HasUsers(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT COUNT(*) FROM users";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    var count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static void AddUser(string connectionString, User user)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var query = "INSERT INTO users (username, password, role, assigned_ip) VALUES (@username, @password, @role, @assigned_ip)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", user.Username);
                    command.Parameters.AddWithValue("password", user.Password); // Upewnij się, że hasła są haszowane w produkcji
                    command.Parameters.AddWithValue("role", user.Role);
                    command.Parameters.AddWithValue("assigned_ip", (object)user.AssignedIp ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static User Authenticate(string connectionString, string username, string password)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT id, username, password, role, assigned_ip FROM users WHERE username = @username AND password = @password";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    command.Parameters.AddWithValue("password", password);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Password = reader.GetString(2),
                                Role = reader.GetString(3),
                                AssignedIp = reader.IsDBNull(4) ? null : reader.GetString(4)
                            };
                        }
                    }
                }
            }
            return null; // Jeśli użytkownik nie istnieje, zwraca null
        }

    }

}
