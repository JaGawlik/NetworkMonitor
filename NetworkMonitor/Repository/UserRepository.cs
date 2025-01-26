using NetworkMonitor.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetworkMonitor.Repository
{
    public static class UserRepository
    {
        /// <summary>
        /// Sprawdza, czy w bazie danych istnieją użytkownicy.
        /// </summary>
        public static async Task<bool> HasUsersAsync(string connectionString)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT COUNT(*) FROM users";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    var count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        /// <summary>
        /// Dodaje nowego użytkownika do bazy danych.
        /// </summary>
        public static async Task AddUserAsync(string connectionString, User user)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO users (username, password, role, assigned_ip) VALUES (@username, @password, @role, @assigned_ip)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", user.Username);
                    command.Parameters.AddWithValue("password", user.Password); // Hasło powinno być wcześniej hashowane
                    command.Parameters.AddWithValue("role", user.Role);
                    command.Parameters.AddWithValue("assigned_ip", (object)user.AssignedIp ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Uwierzytelnia użytkownika na podstawie nazwy użytkownika i hasła.
        /// </summary>
        public static async Task<User> AuthenticateAsync(string connectionString, string username, string password)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT id, username, password, role, assigned_ip FROM users WHERE username = @username AND password = @password";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    command.Parameters.AddWithValue("password", password);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
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
            return null;
        }

        /// <summary>
        /// Pobiera listę wszystkich użytkowników.
        /// </summary>
        public static async Task<List<User>> GetAllUsersAsync(string connectionString)
        {
            var users = new List<User>();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT id, username, role, assigned_ip FROM users";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Role = reader.GetString(2),
                                AssignedIp = reader.IsDBNull(3) ? null : reader.GetString(3)
                            });
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Pobiera użytkownika na podstawie identyfikatora.
        /// </summary>
        public static async Task<User> GetUserByIdAsync(string connectionString, int id)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT id, username, role, assigned_ip FROM users WHERE id = @id";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Role = reader.GetString(2),
                                AssignedIp = reader.IsDBNull(3) ? null : reader.GetString(3)
                            };
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Sprawdza, czy użytkownik o podanej nazwie istnieje w bazie danych.
        /// </summary>
        public static async Task<bool> CheckUserExistsAsync(string connectionString, string username)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT COUNT(*) FROM users WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);

                    var count = (long)await command.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
    }
}
