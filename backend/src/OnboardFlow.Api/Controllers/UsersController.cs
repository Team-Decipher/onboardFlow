using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.DTOs;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Administrator)] // 🔒 Entire Users management is restricted to Administrators
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// GET /api/users?search=...&role=...&status=...
    /// Search and filter users by search term, role, or active status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponseDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status)
    {
        var query = _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .AsNoTracking()
            .AsQueryable();

        // 1. Search filter (email or fullName)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(searchLower) ||
                                     u.Email.ToLower().Contains(searchLower));
        }

        // 2. Role filter
        if (!string.IsNullOrWhiteSpace(role) && !role.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(u => u.Role == role);
        }

        // 3. Status filter ("active", "deactivated", "all")
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive);
            }
            else if (status.Equals("deactivated", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => !u.IsActive);
            }
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                ClientOrganisationId = u.ClientOrganisationId,
                ClientOrganisationName = u.ClientOrganisation != null ? u.ClientOrganisation.Name : null,
                IsActive = u.IsActive,
                CreatedAtUtc = u.CreatedAtUtc,
                UpdatedAtUtc = u.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// GET /api/users/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> GetUserById(Guid id)
    {
        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' was not found." });
        }

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ClientOrganisationId = user.ClientOrganisationId,
            ClientOrganisationName = user.ClientOrganisation?.Name,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });
    }

    /// <summary>
    /// POST /api/users
    /// Creates a new user with Client organisation validation and audit logging
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Full name and email are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check email uniqueness
        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            return Conflict(new { message = $"A user with email '{request.Email}' already exists." });
        }

        // Validate Role
        if (!AppRoles.IsValidRole(request.Role))
        {
            return BadRequest(new { message = $"Role '{request.Role}' is invalid. Allowed roles: {string.Join(", ", AppRoles.All)}" });
        }

        // 🔒 Client Organisation enforcement: Required when Role == Client
        if (AppRoles.RequiresClientOrganisation(request.Role))
        {
            if (!request.ClientOrganisationId.HasValue ||
                !await _dbContext.ClientOrganisations.AnyAsync(o => o.Id == request.ClientOrganisationId.Value))
            {
                return BadRequest(new { message = "Client organisation is required when role is Client." });
            }
        }
        else
        {
            // Internal staff members cannot have a client organisation attached
            request.ClientOrganisationId = null;
        }

        var password = string.IsNullOrWhiteSpace(request.Password) ? "password123" : request.Password;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            ClientOrganisationId = request.ClientOrganisationId,
            PasswordHash = DbSeeder.HashPassword(password),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        // 📝 Write to Audit Log
        _dbContext.AuditLogs.Add(new AuditEntry
        {
            Action = "USER_CREATED",
            ActorId = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? AppRoles.Administrator,
            TargetEntity = "User",
            TargetId = user.Id.ToString(),
            Details = $"Created user '{user.Email}' with role '{user.Role}'.",
            TimestampUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        var orgName = request.ClientOrganisationId.HasValue
            ? (await _dbContext.ClientOrganisations.FindAsync(request.ClientOrganisationId.Value))?.Name
            : null;

        var response = new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ClientOrganisationId = user.ClientOrganisationId,
            ClientOrganisationName = orgName,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, response);
    }

    /// <summary>
    /// PATCH /api/users/{id}
    /// Updates user role, client organisation, profile, or status (active/deactivated) and writes to Audit Log
    /// </summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' was not found." });
        }

        var changes = new List<string>();

        // Update Full Name
        if (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName != user.FullName)
        {
            changes.Add($"FullName: '{user.FullName}' -> '{request.FullName}'");
            user.FullName = request.FullName.Trim();
        }

        // Update Role & Client Organisation
        if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != user.Role)
        {
            if (!AppRoles.IsValidRole(request.Role))
            {
                return BadRequest(new { message = $"Role '{request.Role}' is invalid." });
            }

            changes.Add($"Role: '{user.Role}' -> '{request.Role}'");
            user.Role = request.Role;

            if (AppRoles.RequiresClientOrganisation(user.Role))
            {
                var orgId = request.ClientOrganisationId ?? user.ClientOrganisationId;
                if (!orgId.HasValue || !await _dbContext.ClientOrganisations.AnyAsync(o => o.Id == orgId.Value))
                {
                    return BadRequest(new { message = "Client organisation is required when role is Client." });
                }
                user.ClientOrganisationId = orgId;
            }
            else
            {
                user.ClientOrganisationId = null;
            }
        }
        else if (request.ClientOrganisationId.HasValue && user.Role == AppRoles.Client)
        {
            if (await _dbContext.ClientOrganisations.AnyAsync(o => o.Id == request.ClientOrganisationId.Value))
            {
                changes.Add($"ClientOrganisationId updated to '{request.ClientOrganisationId}'");
                user.ClientOrganisationId = request.ClientOrganisationId;
            }
        }

        // Update Password if provided
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = DbSeeder.HashPassword(request.Password);
            changes.Add("Password updated");
        }

        // 🔒 Status Update & Deactivation Flow
        string auditAction = "USER_UPDATED";
        if (request.IsActive.HasValue && request.IsActive.Value != user.IsActive)
        {
            user.IsActive = request.IsActive.Value;
            auditAction = user.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED";
            changes.Add($"Status changed to {(user.IsActive ? "Active" : "Deactivated")}");
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        // 📝 Write to Audit Log
        _dbContext.AuditLogs.Add(new AuditEntry
        {
            Action = auditAction,
            ActorId = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? AppRoles.Administrator,
            TargetEntity = "User",
            TargetId = user.Id.ToString(),
            Details = $"Updated user '{user.Email}': {string.Join(", ", changes)}",
            TimestampUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            ClientOrganisationId = user.ClientOrganisationId,
            ClientOrganisationName = user.ClientOrganisation?.Name,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });
    }

    /// <summary>
    /// GET /api/users/organisations
    /// List client organisations for dropdown select
    /// </summary>
    [HttpGet("organisations")]
    public async Task<ActionResult<List<ClientOrganisationDto>>> GetClientOrganisations()
    {
        var orgs = await _dbContext.ClientOrganisations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new ClientOrganisationDto
            {
                Id = o.Id,
                Name = o.Name,
                Category = o.Category
            })
            .ToListAsync();

        return Ok(orgs);
    }
}

