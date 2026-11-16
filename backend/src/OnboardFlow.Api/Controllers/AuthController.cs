using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string SignInErrorMessage = "Unable to sign in. Check your email and password, then try again.";

    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            // Single-word name: first 2 chars
            var word = parts[0];
            return word.Length >= 2
                ? word[..2].ToUpperInvariant()
                : word.ToUpperInvariant();
        }

        // Multi-word: first letter of first + first letter of last
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id.ToString(),
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        Company = user.ClientOrganisation?.Name ?? "OnboardFlow Internal",
        ClientOrganisationId = user.ClientOrganisationId,
        ClientOrganisationName = user.ClientOrganisation?.Name,
        Initials = BuildInitials(user.FullName),
        IsActive = user.IsActive
    };

    // ─── POST /api/auth/login ──────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Basic field presence check (400 for missing fields; 401 for everything else)
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // User not found
        if (user == null)
        {
            return Unauthorized(new { message = SignInErrorMessage });
        }

        // Password mismatch
        if (!DbSeeder.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = SignInErrorMessage });
        }

        // Deactivated account
        if (!user.IsActive)
        {
            return Unauthorized(new { message = SignInErrorMessage });
        }

        // Role tab validation: if caller specifies a role it must match the stored role
        if (!string.IsNullOrEmpty(request.Role) &&
            !string.Equals(request.Role, user.Role, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = SignInErrorMessage });
        }

        // Client users must belong to an organisation
        if (user.Role == AppRoles.Client && user.ClientOrganisationId == null)
        {
            return Unauthorized(new { message = SignInErrorMessage });
        }

        var userDto = MapToUserDto(user);

        // Generates JWT containing role, userId, clientOrganisationId, email, and name
        var token = _tokenService.GenerateToken(userDto);

        // Calculate expiry (matches the 8-hour window in TokenService)
        var expiresAt = DateTime.UtcNow.AddHours(8).ToString("o"); // ISO 8601

        // Record audit entry
        _dbContext.AuditLogs.Add(new AuditEntry
        {
            Action = "USER_LOGIN",
            ActorId = user.Id.ToString(),
            ActorEmail = user.Email,
            ActorRole = user.Role,
            TargetEntity = "User",
            TargetId = user.Id.ToString(),
            Details = $"User '{user.Email}' logged in successfully.",
            TimestampUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Message = "Authentication successful",
            User = userDto
        });
    }

    // ─── POST /api/auth/logout ─────────────────────────────────────────────────

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdString = User.FindFirstValue("userId")
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Write audit entry (best-effort; we still return 200 even if claims are missing)
        _dbContext.AuditLogs.Add(new AuditEntry
        {
            Action = "USER_LOGOUT",
            ActorId = userIdString,
            ActorEmail = User.FindFirstValue(ClaimTypes.Email)
                      ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email),
            ActorRole = User.FindFirstValue(ClaimTypes.Role)
                     ?? User.FindFirstValue("role"),
            TargetEntity = "User",
            TargetId = userIdString,
            Details = $"User '{userIdString}' signed out.",
            TimestampUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Signed out successfully." });
    }

    // ─── GET /api/auth/me ─────────────────────────────────────────────────────

    // 🔒 Any active authenticated user can access /me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> GetCurrentUser()
    {
        var userIdString = User.FindFirstValue("userId")
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "User not found or deactivated." });
        }

        return Ok(new
        {
            id = user.Id.ToString(),
            email = user.Email,
            fullName = user.FullName,
            role = user.Role,
            initials = BuildInitials(user.FullName),
            company = user.ClientOrganisation?.Name ?? "OnboardFlow Internal",
            clientOrganisationId = user.ClientOrganisationId,
            clientOrganisationName = user.ClientOrganisation?.Name,
            isActive = user.IsActive
        });
    }

    // ─── Demo role-restricted endpoints (kept as-is) ──────────────────────────

    // 🔒 ONLY Administrator role
    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok(new
        {
            message = "Welcome Administrator! You have access to this restricted area."
        });
    }

    // 🔒 ONLY Reviewers and Onboarding Managers
    [Authorize(Roles = $"{AppRoles.Reviewer},{AppRoles.OnboardingManager}")]
    [HttpGet("reviews-queue")]
    public IActionResult ReviewQueueEndpoint()
    {
        return Ok(new
        {
            message = "Access granted to Reviewers and Onboarding Managers."
        });
    }
}
