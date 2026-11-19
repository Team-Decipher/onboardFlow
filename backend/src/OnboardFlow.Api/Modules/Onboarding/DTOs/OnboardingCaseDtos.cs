using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnboardFlow.Api.Modules.Onboarding.DTOs;

public class CreateOnboardingCaseRequest
{
    [Required]
    public Guid ClientOrganisationId { get; set; }

    public Guid? ClientTypeId { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    public DateTime TargetCompletionDate { get; set; }

    public string? Status { get; set; }
}

public class UpdateOnboardingCaseRequest
{
    public Guid? ClientOrganisationId { get; set; }
    public Guid? ClientTypeId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public string? Status { get; set; }
}

public class OnboardingCaseSummaryDto
{
    public Guid Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public Guid ClientOrganisationId { get; set; }
    public string ClientOrganisationName { get; set; } = string.Empty;
    public Guid ClientTypeId { get; set; }
    public string ClientTypeName { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerShortName { get; set; } = string.Empty;
    public DateTime TargetCompletionDate { get; set; }
    public string DueDateDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = "In review";
    public int ApprovedCount { get; set; }
    public int TotalRequiredCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressDisplay { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class CaseCountsDto
{
    public int All { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
}

public class OnboardingCaseListResponse
{
    public List<OnboardingCaseSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public CaseCountsDto Counts { get; set; } = new();
}

public class OnboardingCaseItemDto
{
    public Guid Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int OrderIndex { get; set; }
    public string Status { get; set; } = "Missing";
    public Guid? AssignedReviewerId { get; set; }
    public string ReviewerName { get; set; } = "Unassigned";
    public DateTime? DueDate { get; set; }
    public string DueDateDisplay { get; set; } = string.Empty;
    public string? UploadedFileName { get; set; }
}

public class CaseTimelineEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed"; // "Completed", "Current", "Pending"
    public string DateDisplay { get; set; } = string.Empty;
}

public class CaseActivityDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string DateDisplay { get; set; } = string.Empty;
}

public class ClientOrganisationBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ClientTypeBriefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? DefaultChecklistTemplateId { get; set; }
    public string? DefaultChecklistTemplateName { get; set; }
    public string? DefaultChecklistTemplateVersion { get; set; }
    public int RequiredCount { get; set; }
    public int OptionalCount { get; set; }
    public List<string> PreviewItems { get; set; } = new();
}

public class CaseOwnerBriefDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CaseFormOptionsDto
{
    public List<ClientOrganisationBriefDto> Organisations { get; set; } = new();
    public List<ClientTypeBriefDto> ClientTypes { get; set; } = new();
    public List<CaseOwnerBriefDto> Owners { get; set; } = new();
}

public class OnboardingCaseDetailDto
{
    public Guid Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;

    public ClientOrganisationBriefDto ClientOrganisation { get; set; } = new();
    public ClientTypeBriefDto ClientType { get; set; } = new();
    public CaseOwnerBriefDto Owner { get; set; } = new();

    public DateTime TargetCompletionDate { get; set; }
    public string DueDateDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = "In review";

    public int ApprovedCount { get; set; }
    public int TotalRequiredCount { get; set; }
    public int TotalCount { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressDisplay { get; set; } = string.Empty;

    public string? AttentionBannerMessage { get; set; }
    public string? AttentionBannerSubtitle { get; set; }

    public List<OnboardingCaseItemDto> Items { get; set; } = new();
    public List<CaseTimelineEventDto> Timeline { get; set; } = new();
    public List<CaseActivityDto> Activities { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
