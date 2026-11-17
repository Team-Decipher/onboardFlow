using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.Checklists.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/checklist-templates")]
public class ChecklistTemplatesController : ControllerBase
{
    private readonly IChecklistTemplateService _service;

    public ChecklistTemplatesController(IChecklistTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetAll([FromQuery] Guid? clientTypeId = null)
    {
        var templates = await _service.GetTemplatesAsync(clientTypeId);
        return Ok(templates);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<ChecklistTemplateDetailDto>> Create([FromBody] CreateChecklistTemplateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = User.FindFirstValue(ClaimTypes.Name) 
                ?? User.FindFirstValue(ClaimTypes.Email) 
                ?? "Administrator";

            var created = await _service.CreateTemplateAsync(dto, user);
            return CreatedAtAction("GetTemplateDetail", "ChecklistItems", new { templateId = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Client Type Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate Template Name",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
