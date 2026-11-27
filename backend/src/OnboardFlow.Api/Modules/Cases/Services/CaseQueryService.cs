using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Cases.DTOs;
using OnboardFlow.Api.Modules.Cases.Repositories;
using OnboardFlow.Api.Modules.Onboarding.Domain;

namespace OnboardFlow.Api.Modules.Cases.Services;

public class CaseQueryService : ICaseQueryService
{
    private readonly ICaseRepository _repository;

    public CaseQueryService(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CaseSummaryDto>> SearchCasesAsync(string? search, string? stage, string? ownerId, string? status)
    {
        var cases = await _repository.GetCasesAsync();

        var query = cases.AsEnumerable();

        // 1. Search filter: Case-insensitive by Case ID (ReferenceCode) or Client Name
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ReferenceCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.ClientOrganisation != null && c.ClientOrganisation.Name.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        // 2. Owner filter: By OwnerUserId Guid or Owner FullName
        if (!string.IsNullOrWhiteSpace(ownerId) && !string.Equals(ownerId, "all", StringComparison.OrdinalIgnoreCase) && !string.Equals(ownerId, "anyone", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(ownerId, out var parsedOwnerGuid))
            {
                query = query.Where(c => c.OwnerUserId == parsedOwnerGuid);
            }
            else
            {
                query = query.Where(c => c.OwnerUser != null && c.OwnerUser.FullName.Contains(ownerId.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        // Map domain cases to DTOs with computed Stage & normalized Status
        var dtoList = query.Select(MapToDto).ToList();

        // 3. Stage filter
        if (!string.IsNullOrWhiteSpace(stage) && !string.Equals(stage, "all", StringComparison.OrdinalIgnoreCase) && !string.Equals(stage, "all stages", StringComparison.OrdinalIgnoreCase))
        {
            dtoList = dtoList.Where(c => string.Equals(c.Stage, stage.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // 4. Status filter: "In progress" vs "Complete"
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase) && !string.Equals(status, "all statuses", StringComparison.OrdinalIgnoreCase))
        {
            var trimmedStatus = status.Trim();
            if (string.Equals(trimmedStatus, "in progress", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedStatus, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                dtoList = dtoList.Where(c => !string.Equals(c.Status, "Complete", StringComparison.OrdinalIgnoreCase) && !string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (string.Equals(trimmedStatus, "complete", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(trimmedStatus, "completed", StringComparison.OrdinalIgnoreCase))
            {
                dtoList = dtoList.Where(c => string.Equals(c.Status, "Complete", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                dtoList = dtoList.Where(c => string.Equals(c.Status, trimmedStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        return dtoList;
    }

    public async Task<CaseFilterOptionsDto> GetFilterOptionsAsync()
    {
        var owners = await _repository.GetCaseOwnersAsync();

        return new CaseFilterOptionsDto
        {
            Stages = new List<string>
            {
                "Document collection",
                "Document review",
                "Sequential approval",
                "Completed"
            },
            Owners = owners.Select(u => new CaseOwnerOptionDto
            {
                Id = u.Id,
                FullName = u.FullName
            }).ToList(),
            Statuses = new List<string>
            {
                "In progress",
                "Complete"
            }
        };
    }

    public async Task<CaseSummaryDto?> GetCaseSummaryByIdAsync(Guid id)
    {
        var onboardingCase = await _repository.GetCaseByIdAsync(id);
        return onboardingCase == null ? null : MapToDto(onboardingCase);
    }

    private static CaseSummaryDto MapToDto(OnboardingCase c)
    {
        var stage = DetermineStage(c);
        var normalizedStatus = DetermineStatus(c);

        return new CaseSummaryDto
        {
            Id = c.Id,
            CaseReference = c.ReferenceCode,
            ClientOrganisationName = c.ClientOrganisation?.Name ?? "Unknown Organisation",
            CaseOwnerName = c.OwnerUser?.FullName ?? "Unassigned",
            StartDate = c.CreatedAtUtc,
            Stage = stage,
            Status = normalizedStatus,
            OwnerUserId = c.OwnerUserId,
            ClientOrganisationId = c.ClientOrganisationId
        };
    }

    private static string DetermineStage(OnboardingCase c)
    {
        if (string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Status, "Complete", StringComparison.OrdinalIgnoreCase))
        {
            return "Completed";
        }

        // If items are present, check item progression
        if (c.Items != null && c.Items.Count > 0)
        {
            var approvedCount = c.Items.Count(i => string.Equals(i.Status, "Approved", StringComparison.OrdinalIgnoreCase));
            var inReviewCount = c.Items.Count(i => string.Equals(i.Status, "In review", StringComparison.OrdinalIgnoreCase));
            var totalCount = c.Items.Count;

            if (approvedCount == totalCount)
            {
                return "Completed";
            }

            if (approvedCount >= totalCount / 2 && inReviewCount > 0)
            {
                return "Sequential approval";
            }

            if (inReviewCount > 0 || approvedCount > 0)
            {
                return "Document review";
            }
        }

        return "Document collection";
    }

    private static string DetermineStatus(OnboardingCase c)
    {
        if (string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Status, "Complete", StringComparison.OrdinalIgnoreCase))
        {
            return "Complete";
        }

        return "In progress";
    }
}
