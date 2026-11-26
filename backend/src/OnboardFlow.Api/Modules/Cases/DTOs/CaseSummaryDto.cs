using System;

namespace OnboardFlow.Api.Modules.Cases.DTOs;

public class CaseSummaryDto
{
    public Guid Id { get; set; }
    public string CaseReference { get; set; } = string.Empty;
    public string ClientOrganisationName { get; set; } = string.Empty;
    public string CaseOwnerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public Guid ClientOrganisationId { get; set; }
}
