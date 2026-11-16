using System;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Domain;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorRole { get; set; }
    public string? TargetEntity { get; set; }
    public string? TargetId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

