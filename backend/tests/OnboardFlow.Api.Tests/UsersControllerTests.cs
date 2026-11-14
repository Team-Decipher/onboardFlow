using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Controllers;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.DTOs;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class UsersControllerTests
{
    private (AppDbContext db, UsersController controller) CreateControllerWithDb(string adminEmail = "admin@onboardflow.com")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var controller = new UsersController(db);
        var adminClaims = new[]
        {
            new Claim("userId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, adminEmail),
            new Claim(ClaimTypes.Role, AppRoles.Administrator)
        };
        var identity = new ClaimsIdentity(adminClaims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return (db, controller);
    }

    [Fact]
    public async Task GetUsers_SupportsFilteringBySearchRoleAndStatus()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var org = new ClientOrganisation
        {
            Id = Guid.NewGuid(),
            Name = "Ceylon Exports",
            ContactEmail = "contact@ceylon.com",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ClientOrganisations.Add(org);

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Admin",
            Email = "alice@onboardflow.com",
            Role = AppRoles.Administrator,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Client",
            Email = "bob@client.com",
            Role = AppRoles.Client,
            ClientOrganisationId = org.Id,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };
        var user3 = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Charlie Inactive",
            Email = "charlie@client.com",
            Role = AppRoles.Client,
            ClientOrganisationId = org.Id,
            PasswordHash = "hash",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(2)
        };

        db.Users.AddRange(user1, user2, user3);
        await db.SaveChangesAsync();

        // Act 1: Search by text
        var searchResult = await controller.GetUsers("alice", null, null);
        var okResult1 = Assert.IsType<OkObjectResult>(searchResult.Result);
        var list1 = Assert.IsType<List<UserResponseDto>>(okResult1.Value);
        Assert.Single(list1);
        Assert.Equal("alice@onboardflow.com", list1[0].Email);

        // Act 2: Filter by Role
        var roleResult = await controller.GetUsers(null, AppRoles.Client, null);
        var okResult2 = Assert.IsType<OkObjectResult>(roleResult.Result);
        var list2 = Assert.IsType<List<UserResponseDto>>(okResult2.Value);
        Assert.Equal(2, list2.Count);

        // Act 3: Filter by status "active"
        var activeResult = await controller.GetUsers(null, null, "active");
        var okResult3 = Assert.IsType<OkObjectResult>(activeResult.Result);
        var list3 = Assert.IsType<List<UserResponseDto>>(okResult3.Value);
        Assert.Equal(2, list3.Count);

        // Act 4: Filter by status "deactivated"
        var deactResult = await controller.GetUsers(null, null, "deactivated");
        var okResult4 = Assert.IsType<OkObjectResult>(deactResult.Result);
        var list4 = Assert.IsType<List<UserResponseDto>>(okResult4.Value);
        Assert.Single(list4);
        Assert.Equal("charlie@client.com", list4[0].Email);
    }

    [Fact]
    public async Task CreateUser_EnforcesClientOrganisation_WhenRoleIsClient()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();

        var requestMissingOrg = new CreateUserRequest
        {
            FullName = "Test Client",
            Email = "testclient@example.com",
            Role = AppRoles.Client,
            ClientOrganisationId = null
        };

        // Act
        var result = await controller.CreateUser(requestMissingOrg);

        // Assert: 400 BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateUser_SucceedsForClient_WithValidOrganisation_AndWritesAuditLog()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb("auditor@onboardflow.com");
        var org = new ClientOrganisation
        {
            Id = Guid.NewGuid(),
            Name = "Ceylon Exports",
            ContactEmail = "ceylon@test.com",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ClientOrganisations.Add(org);
        await db.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            FullName = "John Client",
            Email = "john@client.com",
            Role = AppRoles.Client,
            ClientOrganisationId = org.Id,
            Password = "SecurePassword123!"
        };

        // Act
        var result = await controller.CreateUser(request);

        // Assert: 201 Created
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdUser = Assert.IsType<UserResponseDto>(createdResult.Value);
        Assert.Equal("john@client.com", createdUser.Email);
        Assert.Equal(AppRoles.Client, createdUser.Role);
        Assert.Equal(org.Id, createdUser.ClientOrganisationId);
        Assert.Equal("Ceylon Exports", createdUser.ClientOrganisationName);

        // Assert: Audit log entry written
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetEntity == "User" && a.TargetId == createdUser.Id.ToString());
        Assert.NotNull(auditEntry);
        Assert.Equal("USER_CREATED", auditEntry.Action);
        Assert.Contains(AppRoles.Client, auditEntry.Details);
    }

    [Fact]
    public async Task PatchUser_DeactivatesUser_AndWritesAuditLog()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Active Staff",
            Email = "staff@onboardflow.com",
            Role = AppRoles.Reviewer,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var patchRequest = new UpdateUserRequest
        {
            IsActive = false
        };

        // Act
        var result = await controller.UpdateUser(targetUser.Id, patchRequest);

        // Assert: 200 OK & User deactivated
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedDto = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.False(updatedDto.IsActive);

        // Verify in DB
        var userInDb = await db.Users.FindAsync(targetUser.Id);
        Assert.NotNull(userInDb);
        Assert.False(userInDb.IsActive);

        // Verify Audit Log
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetEntity == "User" && a.TargetId == targetUser.Id.ToString());
        Assert.NotNull(auditEntry);
        Assert.Equal("USER_DEACTIVATED", auditEntry.Action);
        Assert.Contains("Status changed to Deactivated", auditEntry.Details);
    }
}

