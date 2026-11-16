using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Services;

public interface IAuthService
{
    /// <summary>Validates credentials, enforces business rules, and returns a login result.</summary>
    Task<AuthResult> LoginAsync(LoginRequest request);

    /// <summary>Records a sign-out audit entry for the given user ID.</summary>
    Task LogoutAsync(string? userId, string? email, string? role);

    /// <summary>Returns the current user profile by user ID, or null if not found / deactivated.</summary>
    Task<UserDto?> GetCurrentUserAsync(string userId);
}

/// <summary>Discriminated union result returned by <see cref="IAuthService.LoginAsync"/>.</summary>
public sealed class AuthResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? Token { get; private init; }
    public string? ExpiresAt { get; private init; }
    public UserDto? User { get; private init; }

    public static AuthResult Ok(string token, string expiresAt, UserDto user) =>
        new() { Succeeded = true, Token = token, ExpiresAt = expiresAt, User = user };

    public static AuthResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
