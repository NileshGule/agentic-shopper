using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AgenticShopper.Coordinator.Controllers;

/// <summary>
/// Authentication controller for JWT token generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Login endpoint - generates JWT token for testing
    /// In production, this should validate credentials against database
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO: Replace with actual user validation against database
        // For now, this is a mock implementation for testing
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        // Mock validation - accept any non-empty credentials for development
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var role = "Admin"; // Default role

        var token = GenerateJwtToken(userId, familyId, request.Email, role);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("JWT token generated for user: {Email}", request.Email);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60) * 60,
            TokenType = "Bearer",
            UserId = userId,
            FamilyId = familyId,
            Email = request.Email,
            Role = role
        });
    }

    /// <summary>
    /// Refresh token endpoint
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // TODO: Implement refresh token validation and rotation
        // For now, return same response as login
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        // Mock implementation
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var email = "refreshed@example.com";
        var role = "Admin";

        var token = GenerateJwtToken(userId, familyId, email, role);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("JWT token refreshed");

        return Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60) * 60,
            TokenType = "Bearer",
            UserId = userId,
            FamilyId = familyId,
            Email = email,
            Role = role
        });
    }

    /// <summary>
    /// Test endpoint to verify authentication
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var familyId = User.FindFirst("FamilyId")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            FamilyId = familyId,
            Role = role,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    private string GenerateJwtToken(Guid userId, Guid familyId, string email, string role)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("FamilyId", familyId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        // Simple refresh token generation - in production use cryptographically secure random
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid FamilyId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
