using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Techtoon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connStr;

        public AuthController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
                return BadRequest(new { message = "Username and password are required" });

            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand("SELECT id, username, full_name, role, is_active, password_hash FROM users WHERE username = @u", conn);
            cmd.Parameters.AddWithValue("u", request.Username);

            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return Unauthorized(new { message = "User not found" });

            bool isActive = r.IsDBNull(4) ? true : r.GetBoolean(4);
            if (!isActive)
                return Unauthorized(new { message = "Account is disabled" });

            string hash = r.GetString(5);
            if (!BCrypt.Net.BCrypt.Verify(request.Password, hash))
                return Unauthorized(new { message = "Invalid password" });

            return Ok(new
            {
                id = r.GetInt32(0),
                username = r.GetString(1),
                fullName = r.IsDBNull(2) ? "" : r.GetString(2),
                role = r.IsDBNull(3) ? "" : r.GetString(3),
                token = "techtoon-token-" + Guid.NewGuid().ToString() // Simple token for demo
            });
        }
    }
}