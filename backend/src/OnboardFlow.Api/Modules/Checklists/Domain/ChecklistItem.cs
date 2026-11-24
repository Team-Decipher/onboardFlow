using System;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Checklists.Domain;

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChecklistTemplateId { get; set; }
    public ChecklistTemplate? ChecklistTemplate { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public Guid? AssignedReviewerId { get; set; }
    public User? AssignedReviewer { get; set; }

    public int? DueDays { get; set; }
    public DateTime? DueDate { get; set; }

    public int OrderIndex { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System administrator";
}

