using System;
using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>
    /// Unmapped convenience alias for the actor (email or ID) used across services and tests.
    /// Does not alter the database schema.
    /// </summary>
    [NotMapped]
    public string? User
    {
        get => ActorEmail ?? ActorId;
        set => ActorEmail = value;
    }
}

