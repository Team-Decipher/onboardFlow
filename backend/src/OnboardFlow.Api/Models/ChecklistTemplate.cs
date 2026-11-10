using System;
using System.Collections.Generic;

namespace OnboardFlow.Api.Models;

public class ChecklistTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public string? Description { get; set; }
    public int RequiredCount { get; set; } = 0;
    public int OptionalCount { get; set; } = 0;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System administrator";

    public ICollection<ClientType> ClientTypes { get; set; } = new List<ClientType>();
}

