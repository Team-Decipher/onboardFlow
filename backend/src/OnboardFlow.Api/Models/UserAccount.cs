using System;

namespace OnboardFlow.Api.Models;

public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Onboarding Manager", "Reviewer", "Client", "Administrator"
    public string Organisation { get; set; } = string.Empty; // "Internal team", "Ceylon Exports", etc.
    public string PhoneNumber { get; set; } = string.Empty; // "+94 77 123 4567"
    public string Status { get; set; } = "Active"; // "Active", "Deactivated"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Organisation { get; set; } = "Internal team";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
