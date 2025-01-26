using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Repository;
using NetworkMonitor.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiServer.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString;

        public UsersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Logowanie użytkownika.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
                {
                    return BadRequest(new { Message = "Nazwa użytkownika i hasło są wymagane." });
                }

                string hashedPassword = HashPassword(loginRequest.Password);

                var user = await UserRepository.AuthenticateAsync(_connectionString, loginRequest.Username, hashedPassword);

                if (user == null)
                {
                    return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
                }

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Role,
                    user.AssignedIp
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Wystąpił błąd podczas logowania: {ex.Message}" });
            }
        }

        /// <summary>
        /// Dodaj nowego użytkownika.
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest addUserRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(addUserRequest.Username) || string.IsNullOrWhiteSpace(addUserRequest.Password))
                {
                    return BadRequest(new { Message = "Nazwa użytkownika i hasło są wymagane." });
                }

                if (addUserRequest.Password.Length < 8)
                {
                    return BadRequest(new { Message = "Hasło musi mieć co najmniej 8 znaków." });
                }

                var newUser = new User
                {
                    Username = addUserRequest.Username,
                    Password = HashPassword(addUserRequest.Password), // Haszowanie hasła
                    Role = addUserRequest.Role ?? "User",
                    AssignedIp = addUserRequest.AssignedIp
                };

                bool userExists = await UserRepository.CheckUserExistsAsync(_connectionString, newUser.Username);
                if (userExists)
                {
                    return Conflict(new { Message = "Użytkownik o podanej nazwie już istnieje." });
                }

                await UserRepository.AddUserAsync(_connectionString, newUser);

                return Ok(new { Message = "Użytkownik został pomyślnie dodany." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Wystąpił błąd podczas dodawania użytkownika: {ex.Message}" });
            }
        }

        /// <summary>
        /// Pobierz wszystkich użytkowników.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await UserRepository.GetAllUsersAsync(_connectionString);

                if (users == null || users.Count == 0)
                {
                    return Ok(new List<User>());
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Wystąpił błąd podczas pobierania użytkowników: {ex.Message}" });
            }
        }

        /// <summary>
        /// Pobierz szczegóły użytkownika na podstawie ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await UserRepository.GetUserByIdAsync(_connectionString, id);

                if (user == null)
                {
                    return NotFound(new { Message = "Nie znaleziono użytkownika o podanym ID." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Wystąpił błąd podczas pobierania użytkownika: {ex.Message}" });
            }
        }

        // Funkcja hashująca hasło
        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AddUserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? AssignedIp { get; set; }
    }
}
