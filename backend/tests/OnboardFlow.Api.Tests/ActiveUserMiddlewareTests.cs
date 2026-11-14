using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.UsersAndAccess.Authorization;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class ActiveUserMiddlewareTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsActive_CallsNextMiddleware()
    {
        // Arrange
        using var db = CreateDbContext();
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "active@onboardflow.com",
            FullName = "Active User",
            Role = AppRoles.OnboardingManager,
            PasswordHash = "hash",
            IsActive = true
        };
        db.Users.Add(activeUser);
        await db.SaveChangesAsync();

        var wasNextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("userId", activeUser.Id.ToString()),
            new Claim(ClaimTypes.Role, activeUser.Role)
        }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await middleware.InvokeAsync(context, db);

        // Assert
        Assert.True(wasNextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsDeactivated_Returns403AndDoesNotCallNext()
    {
        // Arrange
        using var db = CreateDbContext();
        var deactivatedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "deactivated@onboardflow.com",
            FullName = "Inactive User",
            Role = AppRoles.Reviewer,
            PasswordHash = "hash",
            IsActive = false
        };
        db.Users.Add(deactivatedUser);
        await db.SaveChangesAsync();

        var wasNextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("userId", deactivatedUser.Id.ToString()),
            new Claim(ClaimTypes.Role, deactivatedUser.Role)
        }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await middleware.InvokeAsync(context, db);

        // Assert
        Assert.False(wasNextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthenticated_CallsNextMiddleware()
    {
        // Arrange
        using var db = CreateDbContext();
        var wasNextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ActiveUserMiddleware(next);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context, db);

        // Assert
        Assert.True(wasNextCalled);
    }
}

