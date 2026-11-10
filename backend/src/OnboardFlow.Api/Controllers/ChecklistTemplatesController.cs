using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/checklist-templates")]
public class ChecklistTemplatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChecklistTemplatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetAll()
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
}
