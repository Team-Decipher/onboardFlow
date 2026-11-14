using System;

namespace OnboardFlow.Api.Modules.UsersAndAccess.DTOs;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = "password123";
    public string Role { get; set; } = string.Empty;
    public Guid? ClientOrganisationId { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public Guid? ClientOrganisationId { get; set; }
    public bool? IsActive { get; set; }
    public string? Password { get; set; }
}

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? ClientOrganisationId { get; set; }
    public string? ClientOrganisationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class ClientOrganisationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

