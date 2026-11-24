using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Onboarding.Domain;
using OnboardFlow.Api.Modules.Onboarding.DTOs;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Modules.Onboarding.Services;

public interface IOnboardingCaseService
{
    Task<OnboardingCaseListResponse> GetCasesAsync(
        string? search,
        string? status,
        string? owner,
        string? clientType,
        int page,
        int pageSize,
        ClaimsPrincipal? user = null);

    Task<OnboardingCaseDetailDto?> GetCaseByIdAsync(Guid id, ClaimsPrincipal? user = null);

    Task<(OnboardingCaseDetailDto? Result, string? Error, int StatusCode)> CreateCaseAsync(
        CreateOnboardingCaseRequest request,
        ClaimsPrincipal actor);

    Task<(OnboardingCaseDetailDto? Result, string? Error, int StatusCode)> UpdateCaseAsync(
        Guid id,
        UpdateOnboardingCaseRequest request,
        ClaimsPrincipal actor);

    Task<CaseFormOptionsDto> GetFormOptionsAsync();
}
