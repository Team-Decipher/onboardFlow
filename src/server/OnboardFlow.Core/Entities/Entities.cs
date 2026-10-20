using OnboardFlow.Core.Enums;

namespace OnboardFlow.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public ClientType ClientType { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Draft;
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
}

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public Case? Case { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public ReviewStage CurrentStage { get; set; } = ReviewStage.Legal;
    public DateTime? DueDate { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChecklistItemId { get; set; }
    public ChecklistItem? ChecklistItem { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string BlobUri { get; set; } = string.Empty;
    public int VersionNumber { get; set; } = 1;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? RejectionReason { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
