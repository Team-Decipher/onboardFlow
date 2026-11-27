using System;
using System.Collections.Generic;

namespace OnboardFlow.Api.Models;

public class ClientOrganisation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientCode { get; set; } = string.Empty; // e.g. "CLI-0018"
    public string Name { get; set; } = string.Empty; // "Ceylon Exports (Pvt) Ltd"
    public string City { get; set; } = string.Empty; // "Colombo"
    public string ClientType { get; set; } = "Enterprise"; // "Enterprise", "SME", "Partner"
    public string PrimaryContact { get; set; } = string.Empty; // "Nimali Perera"
    public string ContactEmail { get; set; } = string.Empty; // "nimali@example.com"
    public string PhoneNumber { get; set; } = string.Empty; // "+94 11 234 5678"
    public string BusinessRegistrationNumber { get; set; } = string.Empty; // "PV 123456"
    public string BusinessAddress { get; set; } = string.Empty; // "42 Marine Drive, Colombo 03"
    public int ActiveCasesCount { get; set; } = 0;
    public string CasesSummary { get; set; } = "1 active"; // "1 active", "2 active", "Completed"
    public string Status { get; set; } = "Active"; // "Active", "Inactive", "Archived"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation property
    public List<LinkedCase> LinkedCases { get; set; } = new();
}

public class LinkedCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganisationId { get; set; }
    public string CaseNumber { get; set; } = string.Empty; // e.g. "OBF-2026-018"
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // "Active", "Under Review", "Completed"
    public int ProgressPercentage { get; set; } = 0;
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime? TargetCompletionDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CreateOrganisationRequest
{
    public string? ClientCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; } = string.Empty;
    public string ClientType { get; set; } = "Enterprise";
    public string PrimaryContact { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? BusinessAddress { get; set; }
    public string? CasesSummary { get; set; }
    public string? Status { get; set; } = "Active";
}

public class UpdateOrganisationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ClientCode { get; set; }
    public string? City { get; set; }
    public string ClientType { get; set; } = "Enterprise";
    public string PrimaryContact { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? BusinessAddress { get; set; }
    public string? CasesSummary { get; set; }
    public string? Status { get; set; }
}

public class OrganisationAccessUserDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Client Admin";
    public string Status { get; set; } = "Active";
    public string LastLogin { get; set; } = "Never";
}
