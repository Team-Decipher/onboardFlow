using System;
using System.Collections.Generic;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Onboarding.Domain;

public class OnboardingCase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Sequential or formatted reference code (e.g., "ONB-0248").
    /// </summary>
    public string ReferenceCode { get; set; } = string.Empty;

    public Guid ClientOrganisationId { get; set; }
    public ClientOrganisation? ClientOrganisation { get; set; }

    public Guid ClientTypeId { get; set; }
    public ClientType? ClientType { get; set; }

    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public DateTime TargetCompletionDate { get; set; }

    /// <summary>
    /// Status: "Draft", "In review", "Approved", "Overdue", "Needs correction", "Missing", "Completed"
    /// </summary>
    public string Status { get; set; } = "In review";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "System administrator";

    public ICollection<OnboardingCaseItem> Items { get; set; } = new List<OnboardingCaseItem>();
}
