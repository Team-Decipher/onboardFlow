using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

/// <summary>
/// Implements authentication business logic — user lookup, password verification,
/// role validation, JWT generation, and audit logging — keeping <see cref="AuthController"/>
/// free of data-access and business concerns (loose coupling via <see cref="IAuthService"/>).
/// </summary>
public sealed class AuthService : IAuthService
{
    private const string SignInError =
        "Unable to sign in. Check your email and password, then try again.";

    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext db, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.ClientOrganisation)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // User not found
        if (user == null)
        {
            await WriteAuditAsync("LOGIN_FAILED", null, request.Email, null,
                $"Login failed — email not found: {request.Email}");
            return AuthResult.Fail(SignInError);
        }

        // Password mismatch
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await WriteAuditAsync("LOGIN_FAILED", user.Id.ToString(), user.Email, user.Role,
                $"Login failed — wrong password for '{user.Email}'.");
            return AuthResult.Fail(SignInError);
        }

        // Deactivated account
        if (!user.IsActive)
        {
            await WriteAuditAsync("LOGIN_FAILED", user.Id.ToString(), user.Email, user.Role,
                $"Login failed — account deactivated: '{user.Email}'.");
            return AuthResult.Fail(SignInError);
        }

        // Role tab mismatch
        if (!string.IsNullOrEmpty(request.Role) &&
            !string.Equals(request.Role, user.Role, StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuditAsync("LOGIN_FAILED", user.Id.ToString(), user.Email, user.Role,
                $"Login failed — role mismatch (requested '{request.Role}', actual '{user.Role}').");
            return AuthResult.Fail(SignInError);
        }

        // Client must belong to an organisation
        if (user.Role == AppRoles.Client && user.ClientOrganisationId == null)
        {
            await WriteAuditAsync("LOGIN_FAILED", user.Id.ToString(), user.Email, user.Role,
                $"Login failed — client '{user.Email}' has no linked organisation.");
            return AuthResult.Fail(SignInError);
        }

        // Success path
        var userDto = MapToDto(user);
        var token = _tokenService.GenerateToken(userDto);
        var expiresAt = DateTime.UtcNow.AddHours(8).ToString("o");

        await WriteAuditAsync("USER_LOGIN", user.Id.ToString(), user.Email, user.Role,
            $"User '{user.Email}' logged in successfully.");

        return AuthResult.Ok(token, expiresAt, userDto);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    public async Task LogoutAsync(string? userId, string? email, string? role)
    {
        await WriteAuditAsync("USER_LOGOUT", userId, email, role,
            $"User '{userId}' signed out.");
    }

    // ── Get current user ──────────────────────────────────────────────────────

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var guid))
            return null;

        var user = await _db.Users
            .Include(u => u.ClientOrganisation)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == guid);

        if (user == null || !user.IsActive)
            return null;

        return MapToDto(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserDto MapToDto(User user) => new()
    {
        Id                    = user.Id.ToString(),
        Email                 = user.Email,
        FullName              = user.FullName,
        Role                  = user.Role,
        Company               = user.ClientOrganisation?.Name ?? "OnboardFlow Internal",
        ClientOrganisationId  = user.ClientOrganisationId,
        ClientOrganisationName = user.ClientOrganisation?.Name,
        Initials              = UserDto.BuildInitials(user.FullName),
        IsActive              = user.IsActive
    };

    private async Task WriteAuditAsync(
        string action, string? actorId, string? actorEmail, string? actorRole, string details)
    {
        _db.AuditLogs.Add(new AuditEntry
        {
            Action        = action,
            ActorId       = actorId,
            ActorEmail    = actorEmail,
            ActorRole     = actorRole,
            TargetEntity  = "User",
            TargetId      = actorId,
            Details       = details,
            TimestampUtc  = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
