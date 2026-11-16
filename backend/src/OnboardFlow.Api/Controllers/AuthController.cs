using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            return BadRequest(new { message = "Invalid email or password." });
        }

        // Verify password
        if (!DbSeeder.VerifyPassword(request.Password, user.PasswordHash))
        {
            return BadRequest(new { message = "Invalid email or password." });
        }

        // 🔒 Reject deactivated users at login
        if (!user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Your account has been deactivated. Please contact your administrator."
            });
        }

        var userDto = new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Company = user.ClientOrganisation?.Name ?? "OnboardFlow Internal",
            ClientOrganisationId = user.ClientOrganisationId,
            IsActive = user.IsActive
        };

        // Generates JWT containing role, userId, clientOrganisationId, email, and name
        var token = _tokenService.GenerateToken(userDto);

        // Record audit entry
        _dbContext.AuditLogs.Add(new AuditEntry
        {
            Action = "USER_LOGIN",
            ActorId = user.Id.ToString(),
            ActorEmail = user.Email,
            ActorRole = user.Role,
            TargetEntity = "User",
            TargetId = user.Id.ToString(),
            Details = $"User '{user.Email}' logged in successfully.",
            TimestampUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            Token = token,
            Message = "Authentication successful",
            User = userDto
        });
    }

    // 🔒 Any active authenticated user can access /me
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> GetCurrentUser()
    {
        var userIdString = User.FindFirstValue("userId") 
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity." });
        }

        var user = await _dbContext.Users
            .Include(u => u.ClientOrganisation)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "User not found or deactivated." });
        }

        return Ok(new
        {
            id = user.Id.ToString(),
            email = user.Email,
            fullName = user.FullName,
            role = user.Role,
            company = user.ClientOrganisation?.Name ?? "OnboardFlow Internal",
            clientOrganisationId = user.ClientOrganisationId,
            isActive = user.IsActive,
            message = "Access granted to authenticated user"
        });
    }

    // 🔒 ONLY Administrator role
    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok(new
        {
            message = "Welcome Administrator! You have access to this restricted area."
        });
    }

    // 🔒 ONLY Reviewers and Onboarding Managers
    [Authorize(Roles = $"{AppRoles.Reviewer},{AppRoles.OnboardingManager}")]
    [HttpGet("reviews-queue")]
    public IActionResult ReviewQueueEndpoint()
    {
        return Ok(new
        {
            message = "Access granted to Reviewers and Onboarding Managers."
        });
    }
}
