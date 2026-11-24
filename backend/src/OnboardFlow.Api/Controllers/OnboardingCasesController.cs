using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Onboarding.DTOs;
using OnboardFlow.Api.Modules.Onboarding.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/onboarding/cases")]
[Authorize]
public class OnboardingCasesController : ControllerBase
{
    private readonly IOnboardingCaseService _caseService;

    public OnboardingCasesController(IOnboardingCaseService caseService)
    {
        _caseService = caseService;
    }

    /// <summary>
    /// GET /api/onboarding/cases
    /// Query cases with optional search, status, owner, clientType and pagination.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<OnboardingCaseListResponse>> GetCases(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? owner,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _caseService.GetCasesAsync(search, status, owner, type, page, pageSize, User);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/onboarding/cases/{id}
    /// Retrieve full details, checklist items, timeline and audit activities for a case.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OnboardingCaseDetailDto>> GetCaseById(Guid id)
    {
        var result = await _caseService.GetCaseByIdAsync(id, User);
        if (result == null)
        {
            return NotFound(new { message = $"Onboarding case with ID '{id}' was not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// POST /api/onboarding/cases
    /// Create a new onboarding case, linking organisation/type/owner and instantiating checklist items.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]
    public async Task<ActionResult<OnboardingCaseDetailDto>> CreateCase([FromBody] CreateOnboardingCaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error, statusCode) = await _caseService.CreateCaseAsync(request, User);
        if (error != null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        return CreatedAtAction(nameof(GetCaseById), new { id = result!.Id }, result);
    }

    /// <summary>
    /// PUT /api/onboarding/cases/{id}
    /// Update editable case metadata (owner, target date, client type/org, status).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]
    public async Task<ActionResult<OnboardingCaseDetailDto>> UpdateCase(Guid id, [FromBody] UpdateOnboardingCaseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (result, error, statusCode) = await _caseService.UpdateCaseAsync(id, request, User);
        if (error != null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        return Ok(result);
    }

    /// <summary>
    /// GET /api/onboarding/cases/form-options
    /// Fetch organisations, client types with checklist preview items, and case owners.
    /// </summary>
    [HttpGet("form-options")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]
    public async Task<ActionResult<CaseFormOptionsDto>> GetFormOptions()
    {
        var options = await _caseService.GetFormOptionsAsync();
        return Ok(options);
    }
}
