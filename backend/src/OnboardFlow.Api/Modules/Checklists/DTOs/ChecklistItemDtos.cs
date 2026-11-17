using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnboardFlow.Api.Modules.Checklists.DTOs;

public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public Guid ChecklistTemplateId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public Guid? AssignedReviewerId { get; set; }
    public string? AssignedReviewerName { get; set; }
    public int? DueDays { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateChecklistItemDto
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DocumentName { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public Guid? AssignedReviewerId { get; set; }

    [Range(1, 365, ErrorMessage = "Due days must be between 1 and 365.")]
    public int? DueDays { get; set; }

    public DateTime? DueDate { get; set; }

    public int? OrderIndex { get; set; }
}

public class AssignReviewerDto
{
    public Guid? ReviewerId { get; set; }
}

public class SetItemDueLimitDto
{
    [Range(1, 365, ErrorMessage = "Due days must be between 1 and 365.")]
    public int? DueDays { get; set; }

    public DateTime? DueDate { get; set; }
}

public class UpdateChecklistItemDto
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string DocumentName { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public Guid? AssignedReviewerId { get; set; }

    [Range(1, 365, ErrorMessage = "Due days must be between 1 and 365.")]
    public int? DueDays { get; set; }

    public DateTime? DueDate { get; set; }

    public int OrderIndex { get; set; }
}

public class BulkUpdateChecklistItemsDto
{
    public string? ChecklistName { get; set; }
    public Guid? AssociatedClientTypeId { get; set; }
    public List<UpdateChecklistItemDto> Items { get; set; } = new();
}

public class ReviewerOptionDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ChecklistTemplateDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public string? Description { get; set; }
    public Guid? AssociatedClientTypeId { get; set; }
    public string? AssociatedClientTypeName { get; set; }
    public int RequiredCount { get; set; }
    public int OptionalCount { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = new();
}

