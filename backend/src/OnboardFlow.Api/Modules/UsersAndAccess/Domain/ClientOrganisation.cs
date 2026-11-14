using System;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Domain;

public class ClientOrganisation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Enterprise"; // Enterprise, SME, Partner
    public string? ContactEmail { get; set; }
    public string? ContactName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? BusinessAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<User> Users { get; set; } = new();
}

