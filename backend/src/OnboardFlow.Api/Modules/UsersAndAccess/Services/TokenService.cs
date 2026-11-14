using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(UserDto user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "dev-only-change-me-use-at-least-32-characters";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "OnboardFlow";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "OnboardFlowClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var orgIdString = user.ClientOrganisationId?.ToString() ?? string.Empty;

        // Claims: packed inside the token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new("userId", user.Id), // 👈 Explicit userId claim
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.FullName),
            new(ClaimTypes.Role, user.Role), // 👈 Official .NET Role claim
            new("role", user.Role),          // 👈 Standard JWT role claim for frontend clients
            new("clientOrganisationId", orgIdString), // 👈 Explicit clientOrganisationId claim
            new("company", user.Company ?? string.Empty)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
