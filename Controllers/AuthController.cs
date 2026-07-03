using ADS2026.Data;
using ADS2026.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ADS2026.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Authorize(Roles = "admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Username and password are required." });

        if (await _db.Users.AnyAsync(u => u.Username == req.Username.ToLower()))
            return Conflict(new { message = "Username already exists." });

        var user = new User
        {
            Username = req.Username.ToLower(),
            PasswordHash = HashPassword(req.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User registered successfully." });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Role, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { message = "New password is required." });

        if (req.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.PasswordHash = HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Password changed for '{user.Username}'." });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (user.Id == currentUserId)
            return BadRequest(new { message = "Cannot delete your own account." });

        if (user.Role == "admin")
            return BadRequest(new { message = "Cannot delete an admin user." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User '{user.Username}' deleted." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Username and password are required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username.ToLower());

        if (user == null || user.PasswordHash != HashPassword(req.Password))
            return Unauthorized(new { message = "Invalid username or password." });

        var token = GenerateJwt(user);

        return Ok(new { token, username = user.Username, role = user.Role });
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public class AuthRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string NewPassword { get; set; }
    }
}
