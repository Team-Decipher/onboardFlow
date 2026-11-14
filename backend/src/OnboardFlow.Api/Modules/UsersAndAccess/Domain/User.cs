using System;
using OnboardFlow.Api.Common.Authorization;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Client;
    public Guid? ClientOrganisationId { get; set; }
    public ClientOrganisation? ClientOrganisation { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

