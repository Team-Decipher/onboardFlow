using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "unit-test-secret-key-at-least-32-characters-long-123456" },
            { "Jwt:Issuer", "OnboardFlowTest" },
            { "Jwt:Audience", "OnboardFlowTestAudience" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_IncludesRequiredClaims_ForClientUser()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var userDto = new UserDto
        {
            Id = Guid.NewGuid().ToString(),
            Email = "client@example.com",
            FullName = "Jane Client",
            Role = AppRoles.Client,
            ClientOrganisationId = orgId,
            Company = "Acme Corp"
        };

        // Act
        var tokenString = _tokenService.GenerateToken(userDto);

        // Assert
        Assert.NotNull(tokenString);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        // Verify claims
        Assert.Equal("OnboardFlowTest", jwtToken.Issuer);
        Assert.Contains("OnboardFlowTestAudience", jwtToken.Audiences);

        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
        Assert.NotNull(userIdClaim);
        Assert.Equal(userDto.Id, userIdClaim.Value);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal("client@example.com", emailClaim.Value);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(AppRoles.Client, roleClaim.Value);

        var orgClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "clientOrganisationId");
        Assert.NotNull(orgClaim);
        Assert.Equal(orgId.ToString(), orgClaim.Value);
    }

    [Fact]
    public void GenerateToken_OmitsClientOrganisation_ForInternalUsers()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@onboardflow.com",
            FullName = "System Admin",
            Role = AppRoles.Administrator,
            ClientOrganisationId = null
        };

        // Act
        var tokenString = _tokenService.GenerateToken(userDto);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal(AppRoles.Administrator, roleClaim.Value);

        var orgClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "clientOrganisationId");
        Assert.NotNull(orgClaim);
        Assert.Equal(string.Empty, orgClaim.Value);
    }
}

