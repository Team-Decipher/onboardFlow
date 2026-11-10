using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/client-types")]
public class ClientTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientTypesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientTypeDto>>> GetAll()
    {
        var clientTypes = await _context.ClientTypes
            .Include(c => c.DefaultChecklistTemplate)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync();

        var result = clientTypes.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientTypeDto>> GetById(Guid id)
    {
        var clientType = await _context.ClientTypes
            .Include(c => c.DefaultChecklistTemplate)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clientType == null)
        {
            return NotFound(new { message = $"Client type with ID '{id}' was not found." });
        }

        return Ok(MapToDto(clientType));
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetTemplates()
    {
        var templates = await _context.ChecklistTemplates
            .OrderBy(t => t.Name)
            .Select(t => new ChecklistTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Version = t.Version,
                Description = t.Description,
                RequiredCount = t.RequiredCount,
                OptionalCount = t.OptionalCount
            })
            .ToListAsync();

        return Ok(templates);
    }

    [HttpPost]
    public async Task<ActionResult<ClientTypeDto>> Create([FromBody] CreateClientTypeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _context.ClientTypes.AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower()))
        {
            return BadRequest(new { message = $"A client type named '{dto.Name}' already exists." });
        }

        if (dto.DefaultChecklistTemplateId.HasValue)
        {
            var templateExists = await _context.ChecklistTemplates.AnyAsync(t => t.Id == dto.DefaultChecklistTemplateId.Value);
            if (!templateExists)
            {
                return BadRequest(new { message = "Selected checklist template does not exist." });
            }
        }

        var clientType = new ClientType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            DefaultChecklistTemplateId = dto.DefaultChecklistTemplateId,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "System administrator"
        };

        _context.ClientTypes.Add(clientType);
        await _context.SaveChangesAsync();

        // Reload with navigation property
        await _context.Entry(clientType)
            .Reference(c => c.DefaultChecklistTemplate)
            .LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = clientType.Id }, MapToDto(clientType));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientTypeDto>> Update(Guid id, [FromBody] UpdateClientTypeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var clientType = await _context.ClientTypes
            .Include(c => c.DefaultChecklistTemplate)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clientType == null)
        {
            return NotFound(new { message = $"Client type with ID '{id}' was not found." });
        }

        // Check name uniqueness if changed
        if (!string.Equals(clientType.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.ClientTypes.AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.Trim().ToLower()))
            {
                return BadRequest(new { message = $"Another client type named '{dto.Name}' already exists." });
            }
        }

        if (dto.DefaultChecklistTemplateId.HasValue)
        {
            var templateExists = await _context.ChecklistTemplates.AnyAsync(t => t.Id == dto.DefaultChecklistTemplateId.Value);
            if (!templateExists)
            {
                return BadRequest(new { message = "Selected checklist template does not exist." });
            }
        }

        clientType.Name = dto.Name.Trim();
        if (dto.Description != null)
        {
            clientType.Description = dto.Description.Trim();
        }
        clientType.DefaultChecklistTemplateId = dto.DefaultChecklistTemplateId;

        await _context.SaveChangesAsync();

        // Reload reference if template ID changed
        await _context.Entry(clientType)
            .Reference(c => c.DefaultChecklistTemplate)
            .LoadAsync();

        return Ok(MapToDto(clientType));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clientType = await _context.ClientTypes.FindAsync(id);
        if (clientType == null)
        {
            return NotFound(new { message = $"Client type with ID '{id}' was not found." });
        }

        _context.ClientTypes.Remove(clientType);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static ClientTypeDto MapToDto(ClientType c)
    {
        return new ClientTypeDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            DefaultChecklistTemplateId = c.DefaultChecklistTemplateId,
            DefaultChecklistTemplateName = c.DefaultChecklistTemplate?.Name,
            DefaultChecklistTemplateVersion = c.DefaultChecklistTemplate?.Version,
            DefaultChecklistTemplateDisplayName = c.DefaultChecklistTemplate != null
                ? $"{c.DefaultChecklistTemplate.Name} · {c.DefaultChecklistTemplate.Version}"
                : "No checklist assigned",
            RequiredCount = c.DefaultChecklistTemplate?.RequiredCount ?? 0,
            OptionalCount = c.DefaultChecklistTemplate?.OptionalCount ?? 0,
            CreatedAtUtc = c.CreatedAtUtc,
            CreatedBy = c.CreatedBy
        };
    }
}
