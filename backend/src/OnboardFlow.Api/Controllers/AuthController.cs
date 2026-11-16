using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;

namespace OnboardFlow.Api.Controllers;

/// <summary>
/// Thin HTTP adapter — delegates all business logic to <see cref="IAuthService"/>.
/// No data-access, no password hashing, no JWT construction here (loose coupling).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ─── POST /api/auth/login ──────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var result = await _authService.LoginAsync(request);

        if (!result.Succeeded)
            return Unauthorized(new { message = result.ErrorMessage });

        return Ok(new LoginResponse
        {
            Token     = result.Token!,
            ExpiresAt = result.ExpiresAt!,
            Message   = "Authentication successful",
            User      = result.User!
        });
    }

    // ─── POST /api/auth/logout ─────────────────────────────────────────────────

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email  = User.FindFirstValue(ClaimTypes.Email)
                  ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email);
        var role   = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        await _authService.LogoutAsync(userId, email, role);
        return Ok(new { message = "Signed out successfully." });
    }

    // ─── GET /api/auth/me ─────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> GetCurrentUser()
    {
        var userIdString = User.FindFirstValue("userId")
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized(new { message = "Invalid user identity." });

        var user = await _authService.GetCurrentUserAsync(userIdString);

        if (user == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "User not found or deactivated." });

        return Ok(new
        {
            id                   = user.Id,
            email                = user.Email,
            fullName             = user.FullName,
            role                 = user.Role,
            initials             = user.Initials,
            company              = user.Company,
            clientOrganisationId = user.ClientOrganisationId,
            clientOrganisationName = user.ClientOrganisationName,
            isActive             = user.IsActive
        });
    }

    // ─── Demo role-restricted endpoints (kept as-is) ──────────────────────────

    [Authorize(Roles = AppRoles.Administrator)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnlyEndpoint() =>
        Ok(new { message = "Welcome Administrator! You have access to this restricted area." });

    [Authorize(Roles = $"{AppRoles.Reviewer},{AppRoles.OnboardingManager}")]
    [HttpGet("reviews-queue")]
    public IActionResult ReviewQueueEndpoint() =>
        Ok(new { message = "Access granted to Reviewers and Onboarding Managers." });
}
