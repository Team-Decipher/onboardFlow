using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OnboardFlow.Core.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OnboardFlow.API.Controllers;

public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        // Demo credential validation
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var secret = _configuration["JwtSettings:SecretKey"] ?? "DefaultFallbackSecretKeyWith32CharsLong!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, req.Email),
            new Claim(JwtRegisteredClaimNames.Email, req.Email),
            new Claim(ClaimTypes.Role, UserRole.OnboardingManager.ToString()),
            new Claim("role", UserRole.OnboardingManager.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new
            {
                email = req.Email,
                role = UserRole.OnboardingManager.ToString(),
            }
        });
    }
}
