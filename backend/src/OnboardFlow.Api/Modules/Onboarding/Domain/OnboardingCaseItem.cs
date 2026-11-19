using System;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Onboarding.Domain;

public class OnboardingCaseItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingCaseId { get; set; }
    public OnboardingCase? OnboardingCase { get; set; }

    public string DocumentName { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public int OrderIndex { get; set; } = 1;

    /// <summary>
    /// Item status: "Approved", "In review", "Needs correction", "Missing"
    /// </summary>
    public string Status { get; set; } = "Missing";

    public Guid? AssignedReviewerId { get; set; }
    public User? AssignedReviewer { get; set; }

    /// <summary>
    /// Reviewer display tag, e.g. "Legal", "Finance", or reviewer name
    /// </summary>
    public string? ReviewerName { get; set; }

    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Uploaded file reference if present (e.g., "business-registration.pdf")
    /// </summary>
    public string? UploadedFileName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
