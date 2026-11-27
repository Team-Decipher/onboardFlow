using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganisationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrganisationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientOrganisation>>> GetOrganisations(
        [FromQuery] string? search,
        [FromQuery] string? clientType,
        [FromQuery] string? status)
    {
        var query = _context.Organisations
            .Include(o => o.LinkedCases)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(s) ||
                o.ClientCode.ToLower().Contains(s) ||
                o.City.ToLower().Contains(s) ||
                o.PrimaryContact.ToLower().Contains(s) ||
                o.ContactEmail.ToLower().Contains(s) ||
                o.BusinessRegistrationNumber.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(clientType) && !clientType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.ClientType.ToLower() == clientType.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.Status.ToLower() == status.Trim().ToLower());
        }

        var list = await query
            .OrderByDescending(o => o.ClientCode)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientOrganisation>> GetOrganisation(string id)
    {
        ClientOrganisation? org = null;

        if (Guid.TryParse(id, out var guid))
        {
            org = await _context.Organisations
                .Include(o => o.LinkedCases)
                .FirstOrDefaultAsync(o => o.Id == guid);
        }

        if (org == null)
        {
            var normalizedId = id.Trim().ToLower();
            org = await _context.Organisations
                .Include(o => o.LinkedCases)
                .FirstOrDefaultAsync(o => o.ClientCode.ToLower() == normalizedId);
        }

        if (org == null)
        {
            return NotFound(new { message = $"Organisation with ID or Code '{id}' was not found." });
        }

        return Ok(org);
    }

    [HttpPost]
    public async Task<ActionResult<ClientOrganisation>> CreateOrganisation([FromBody] CreateOrganisationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Organisation name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryContact))
        {
            return BadRequest(new { message = "Primary contact name is required." });
        }

        if (!IsValidContactEmail(request.ContactEmail))
        {
            return BadRequest(new { message = "A valid contact email is required." });
        }

        // Generate ClientCode if not given (e.g. CLI-0019)
        var clientCode = request.ClientCode?.Trim();
        if (string.IsNullOrWhiteSpace(clientCode))
        {
            var existingCodes = await _context.Organisations
                .Select(o => o.ClientCode)
                .ToListAsync();
            var highestCodeNumber = existingCodes
                .Select(code =>
                {
                    var suffix = code.StartsWith("CLI-", StringComparison.OrdinalIgnoreCase)
                        ? code[4..]
                        : string.Empty;
                    return int.TryParse(suffix, out var number) ? number : 18;
                })
                .DefaultIfEmpty(18)
                .Max();
            clientCode = $"CLI-{highestCodeNumber + 1:D4}";
        }

        var existingCode = await _context.Organisations
            .FirstOrDefaultAsync(o => o.ClientCode.ToLower() == clientCode.ToLower());
        if (existingCode != null)
        {
            return Conflict(new { message = $"Organisation code '{clientCode}' is already registered." });
        }

        var org = new ClientOrganisation
        {
            Id = Guid.NewGuid(),
            ClientCode = clientCode,
            Name = request.Name.Trim(),
            City = request.City?.Trim() ?? "Colombo",
            ClientType = string.IsNullOrWhiteSpace(request.ClientType) ? "Enterprise" : request.ClientType.Trim(),
            PrimaryContact = request.PrimaryContact.Trim(),
            ContactEmail = request.ContactEmail?.Trim() ?? string.Empty,
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            BusinessRegistrationNumber = request.BusinessRegistrationNumber?.Trim() ?? string.Empty,
            BusinessAddress = request.BusinessAddress?.Trim() ?? string.Empty,
            CasesSummary = string.IsNullOrWhiteSpace(request.CasesSummary) ? "1 active" : request.CasesSummary.Trim(),
            ActiveCasesCount = 1,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Create an initial linked onboarding case
        org.LinkedCases.Add(new LinkedCase
        {
            Id = Guid.NewGuid(),
            OrganisationId = org.Id,
            CaseNumber = $"OBF-2026-{new Random().Next(100, 999)}",
            Title = $"{org.Name} Onboarding Workflow",
            Status = "Active",
            ProgressPercentage = 10,
            ReviewerName = "Kasun Jayawardena",
            TargetCompletionDate = DateTime.UtcNow.AddDays(14),
            CreatedAtUtc = DateTime.UtcNow
        });

        _context.Organisations.Add(org);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganisation), new { id = org.Id }, org);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientOrganisation>> UpdateOrganisation(string id, [FromBody] UpdateOrganisationRequest request)
    {
        ClientOrganisation? org = null;

        if (Guid.TryParse(id, out var guid))
        {
            org = await _context.Organisations
                .Include(o => o.LinkedCases)
                .FirstOrDefaultAsync(o => o.Id == guid);
        }

        if (org == null)
        {
            var normalizedId = id.Trim().ToLower();
            org = await _context.Organisations
                .Include(o => o.LinkedCases)
                .FirstOrDefaultAsync(o => o.ClientCode.ToLower() == normalizedId);
        }

        if (org == null)
        {
            return NotFound(new { message = $"Organisation with ID or Code '{id}' was not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Organisation name is required." });
        }

        if (!IsValidContactEmail(request.ContactEmail))
        {
            return BadRequest(new { message = "A valid contact email is required." });
        }

        org.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.ClientCode))
        {
            org.ClientCode = request.ClientCode.Trim();
        }
        if (request.City != null)
        {
            org.City = request.City.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.ClientType))
        {
            org.ClientType = request.ClientType.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.PrimaryContact))
        {
            org.PrimaryContact = request.PrimaryContact.Trim();
        }
        if (request.ContactEmail != null)
        {
            org.ContactEmail = request.ContactEmail.Trim();
        }
        if (request.PhoneNumber != null)
        {
            org.PhoneNumber = request.PhoneNumber.Trim();
        }
        if (request.BusinessRegistrationNumber != null)
        {
            org.BusinessRegistrationNumber = request.BusinessRegistrationNumber.Trim();
        }
        if (request.BusinessAddress != null)
        {
            org.BusinessAddress = request.BusinessAddress.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.CasesSummary))
        {
            org.CasesSummary = request.CasesSummary.Trim();
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            org.Status = request.Status.Trim();
        }

        org.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(org);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganisation(string id)
    {
        ClientOrganisation? org = null;

        if (Guid.TryParse(id, out var guid))
        {
            org = await _context.Organisations.FindAsync(guid);
        }

        if (org == null)
        {
            var normalizedId = id.Trim().ToLower();
            org = await _context.Organisations.FirstOrDefaultAsync(o => o.ClientCode.ToLower() == normalizedId);
        }

        if (org == null)
        {
            return NotFound(new { message = $"Organisation with ID or Code '{id}' was not found." });
        }

        _context.Organisations.Remove(org);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/cases")]
    public async Task<ActionResult<IEnumerable<LinkedCase>>> GetOrganisationCases(string id)
    {
        Guid orgId = Guid.Empty;
        if (Guid.TryParse(id, out var parsedGuid))
        {
            orgId = parsedGuid;
        }
        else
        {
            var normalizedId = id.Trim().ToLower();
            var org = await _context.Organisations.FirstOrDefaultAsync(o => o.ClientCode.ToLower() == normalizedId);
            if (org != null)
            {
                orgId = org.Id;
            }
        }

        if (orgId == Guid.Empty)
        {
            return NotFound(new { message = $"Organisation with identifier '{id}' not found." });
        }

        var cases = await _context.LinkedCases
            .Where(c => c.OrganisationId == orgId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        return Ok(cases);
    }

    [HttpGet("{id}/access")]
    public async Task<ActionResult<IEnumerable<OrganisationAccessUserDto>>> GetOrganisationAccess(string id)
    {
        ClientOrganisation? org = null;
        if (Guid.TryParse(id, out var parsedGuid))
        {
            org = await _context.Organisations.FirstOrDefaultAsync(o => o.Id == parsedGuid);
        }
        else
        {
            var normalizedId = id.Trim().ToLower();
            org = await _context.Organisations.FirstOrDefaultAsync(o => o.ClientCode.ToLower() == normalizedId);
        }

        if (org == null)
        {
            return NotFound(new { message = $"Organisation with identifier '{id}' not found." });
        }

        var accessUsers = new List<OrganisationAccessUserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FullName = org.PrimaryContact,
                Email = org.ContactEmail,
                Role = "Primary Client Admin",
                Status = "Active",
                LastLogin = "Today at 09:14 AM"
            },
            new()
            {
                Id = Guid.NewGuid(),
                FullName = "Finance Department (" + org.Name + ")",
                Email = "billing@" + (org.ContactEmail.Contains('@') ? org.ContactEmail.Split('@')[1] : "client.com"),
                Role = "Document Submitter",
                Status = "Active",
                LastLogin = "Yesterday at 04:30 PM"
            }
        };

        return Ok(accessUsers);
    }

    private static bool IsValidContactEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        new EmailAddressAttribute().IsValid(email.Trim());
}
