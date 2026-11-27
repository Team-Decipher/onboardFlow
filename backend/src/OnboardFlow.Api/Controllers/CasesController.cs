using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Modules.Cases.DTOs;
using OnboardFlow.Api.Modules.Cases.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CasesController : ControllerBase
{
    private readonly ICaseQueryService _queryService;

    public CasesController(ICaseQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// GET /api/cases?search=&stage=&ownerId=&status=
    /// Searches cases with case-insensitive search by Case ID or Client Name, plus optional Stage, Owner, and Status filters.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CaseSummaryDto>>> GetCases(
        [FromQuery] string? search = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? status = null)
    {
        var cases = await _queryService.SearchCasesAsync(search, stage, ownerId, status);
        return Ok(cases);
    }

    /// <summary>
    /// GET /api/cases/filters
    /// Returns available dropdown filters for stages, owners, and statuses.
    /// </summary>
    [HttpGet("filters")]
    public async Task<ActionResult<CaseFilterOptionsDto>> GetFilters()
    {
        var filters = await _queryService.GetFilterOptionsAsync();
        return Ok(filters);
    }

    /// <summary>
    /// GET /api/cases/{id}
    /// Retrieves a single case summary by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaseSummaryDto>> GetCaseById(Guid id)
    {
        var caseSummary = await _queryService.GetCaseSummaryByIdAsync(id);
        if (caseSummary == null)
        {
            return NotFound(new { message = $"Case with ID '{id}' was not found." });
        }

        return Ok(caseSummary);
    }
}
