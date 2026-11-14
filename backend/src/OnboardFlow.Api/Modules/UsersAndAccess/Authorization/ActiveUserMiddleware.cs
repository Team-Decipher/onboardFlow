using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Data;

namespace OnboardFlow.Api.Modules.UsersAndAccess.Authorization;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdString = context.User.FindFirstValue("userId") 
                            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdString, out var userId))
            {
                var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null && !user.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Your account has been deactivated. Please contact your administrator."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}

