using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Repository;
using NetworkMonitor.AppConfiguration;

namespace ApiServer.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var user = UserRepository.Authenticate(_connectionString, loginRequest.Username, loginRequest.Password);

                if (user == null)
                {
                    return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Wystąpił błąd podczas logowania: {ex.Message}" });
            }
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
