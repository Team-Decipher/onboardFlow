using System;
using System.Collections.Generic;

namespace OnboardFlow.Api.Modules.Cases.DTOs;

public class CaseOwnerOptionDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class CaseFilterOptionsDto
{
    public List<string> Stages { get; set; } = new();
    public List<CaseOwnerOptionDto> Owners { get; set; } = new();
    public List<string> Statuses { get; set; } = new();
}
