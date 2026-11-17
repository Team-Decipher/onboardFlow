using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.Checklists.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/checklist-templates")]
public class ChecklistItemsController : ControllerBase
{
    private readonly IChecklistItemService _service;

    public ChecklistItemsController(IChecklistItemService service)
    {
        _service = service;
    }

    [HttpGet("{templateId:guid}/items")]
    public async Task<ActionResult<IEnumerable<ChecklistItemDto>>> GetItems(Guid templateId)
    {
        var items = await _service.GetItemsByTemplateIdAsync(templateId);
        return Ok(items);
    }

    [HttpGet("{templateId:guid}")]
    public async Task<ActionResult<ChecklistTemplateDetailDto>> GetTemplateDetail(Guid templateId)
    {
        var detail = await _service.GetTemplateDetailAsync(templateId);
        if (detail == null)
        {
            return NotFound(new { message = $"Checklist template with ID '{templateId}' was not found." });
        }
        return Ok(detail);
    }

    [HttpGet("reviewers")]
    public async Task<ActionResult<IEnumerable<ReviewerOptionDto>>> GetReviewers()
    {
        var reviewers = await _service.GetReviewersAsync();
        return Ok(reviewers);
    }

    [HttpPost("{templateId:guid}/items")]
    public async Task<ActionResult<ChecklistItemDto>> AddItem(
        Guid templateId, 
        [FromBody] CreateChecklistItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Administrator";
            var created = await _service.AddItemAsync(templateId, dto, user);
            return CreatedAtAction(nameof(GetItems), new { templateId }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{templateId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid templateId, Guid itemId)
    {
        var success = await _service.RemoveItemAsync(templateId, itemId);
        if (!success)
        {
            return NotFound(new { message = $"Checklist item with ID '{itemId}' not found on template '{templateId}'." });
        }

        return NoContent();
    }

    [HttpPut("{templateId:guid}/items/bulk")]
    public async Task<ActionResult<IEnumerable<ChecklistItemDto>>> BulkSaveItems(
        Guid templateId, 
        [FromBody] BulkUpdateChecklistItemsDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Administrator";
            var updated = await _service.BulkSaveItemsAsync(templateId, dto, user);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{templateId:guid}/items/{itemId:guid}/reviewer")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.OnboardingManager}")]
    public async Task<ActionResult<ChecklistItemDto>> AssignReviewer(
        Guid templateId, 
        Guid itemId, 
        [FromBody] AssignReviewerDto dto)
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

            var updated = await _service.AssignReviewerAsync(templateId, itemId, dto.ReviewerId, user);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Reviewer",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item or Template Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpPut("{templateId:guid}/items/{itemId:guid}/due")]
    [Authorize(Roles = AppRoles.Administrator)]
    public async Task<ActionResult<ChecklistItemDto>> SetItemDue(
        Guid templateId, 
        Guid itemId, 
        [FromBody] SetItemDueLimitDto dto)
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

            var updated = await _service.SetItemDueLimitAsync(templateId, itemId, dto, user);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Due Limit",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Item or Template Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}

