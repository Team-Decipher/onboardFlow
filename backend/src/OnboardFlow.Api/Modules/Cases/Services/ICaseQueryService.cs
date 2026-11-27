using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnboardFlow.Api.Modules.Cases.DTOs;

namespace OnboardFlow.Api.Modules.Cases.Services;

public interface ICaseQueryService
{
    Task<IReadOnlyList<CaseSummaryDto>> SearchCasesAsync(string? search, string? stage, string? ownerId, string? status);
    Task<CaseFilterOptionsDto> GetFilterOptionsAsync();
    Task<CaseSummaryDto?> GetCaseSummaryByIdAsync(Guid id);
}
