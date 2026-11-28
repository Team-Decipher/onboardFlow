using System;
using System.Collections.Generic;

namespace OnboardFlow.Api.Modules.Documents.Domain;

/// <summary>
/// Represents a document uploaded for a specific onboarding case and checklist item.
/// </summary>
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Guid ChecklistItemId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}
