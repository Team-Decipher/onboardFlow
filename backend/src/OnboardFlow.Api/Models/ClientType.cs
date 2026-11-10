using System;

namespace OnboardFlow.Api.Models;

public class ClientType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? DefaultChecklistTemplateId { get; set; }
    public ChecklistTemplate? DefaultChecklistTemplate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System administrator";
}
