using System;

namespace OnboardFlow.Api.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>
    /// Optional: when supplied, the authenticated user's role must match this value or login is rejected (401).
    /// Used by role-specific login tabs in the UI.
    /// </summary>
    public string? Role { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public Guid? ClientOrganisationId { get; set; }
    public string? ClientOrganisationName { get; set; }
    public string Initials { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    /// <summary>ISO 8601 timestamp – exactly 8 hours after the token was issued.</summary>
    public string ExpiresAt { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
