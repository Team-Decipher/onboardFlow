using System;
using System.ComponentModel.DataAnnotations;

namespace OnboardFlow.Api.Models;

public class ClientTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? DefaultChecklistTemplateId { get; set; }
    public string? DefaultChecklistTemplateName { get; set; }
    public string? DefaultChecklistTemplateVersion { get; set; }
    public string? DefaultChecklistTemplateDisplayName { get; set; }
    public int RequiredCount { get; set; }
    public int OptionalCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateClientTypeDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? DefaultChecklistTemplateId { get; set; }
}

public class UpdateClientTypeDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? DefaultChecklistTemplateId { get; set; }
}

public class ChecklistTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public string DisplayName => $"{Name} · {Version}";
    public string? Description { get; set; }
    public int RequiredCount { get; set; }
    public int OptionalCount { get; set; }
}
