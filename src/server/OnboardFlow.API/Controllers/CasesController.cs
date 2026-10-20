using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Application.DTOs;
using OnboardFlow.Application.Services;

namespace OnboardFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;

    public CasesController(ICaseService caseService)
    {
        _caseService = caseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cases = await _caseService.GetAllCasesAsync();
        return Ok(cases);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var c = await _caseService.GetCaseByIdAsync(id);
        if (c == null) return NotFound(new { message = $"Case with ID {id} was not found." });
        return Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCaseDto dto)
    {
        var created = await _caseService.CreateCaseAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
