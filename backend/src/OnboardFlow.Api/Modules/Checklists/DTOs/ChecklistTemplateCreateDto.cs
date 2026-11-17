using System;
using System.ComponentModel.DataAnnotations;

namespace OnboardFlow.Api.Modules.Checklists.DTOs;

public class CreateChecklistTemplateDto
{
    [Required(ErrorMessage = "Checklist template name is required.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Template name must be between 3 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Associated client type ID is required.")]
    public Guid ClientTypeId { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [StringLength(20, ErrorMessage = "Version cannot exceed 20 characters.")]
    public string? Version { get; set; } = "v1";
}
