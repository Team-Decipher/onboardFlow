using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Controllers;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Checklists.Domain;
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.Checklists.Repositories;
using OnboardFlow.Api.Modules.Checklists.Services;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class ChecklistItemReviewerTests
{
    private (AppDbContext db, ChecklistItemsController controller) CreateControllerWithDb(
        string userRole = AppRoles.Administrator,
        string userEmail = "admin@onboardflow.com")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var repo = new ChecklistItemRepository(db);
        var service = new ChecklistItemService(repo, db);
        var controller = new ChecklistItemsController(service);

        var claims = new[]
        {
            new Claim("userId", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, userEmail),
            new Claim(ClaimTypes.Name, userEmail),
            new Claim(ClaimTypes.Role, userRole)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
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
    public async Task AssignReviewer_AsAdministrator_ValidReviewer_Returns200AndUpdatesItem()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb(AppRoles.Administrator, "admin@test.com");
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Enterprise Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Certificate of Incorporation",
            IsRequired = true,
            OrderIndex = 1
        };
        db.ChecklistItems.Add(item);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Amara Silva",
            Email = "amara@test.com",
            Role = AppRoles.Reviewer,
            IsActive = true
        };
        db.Users.Add(reviewer);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = reviewer.Id };

        // Act
        var result = await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<ChecklistItemDto>(okResult.Value);
        Assert.Equal(reviewer.Id, updated.AssignedReviewerId);
        Assert.Equal("Amara Silva", updated.AssignedReviewerName);

        // Verify DB entity updated
        var itemInDb = await db.ChecklistItems.FindAsync(item.Id);
        Assert.NotNull(itemInDb);
        Assert.Equal(reviewer.Id, itemInDb.AssignedReviewerId);
    }

    [Fact]
    public async Task AssignReviewer_AsOnboardingManager_ValidReviewer_Returns200()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb(AppRoles.OnboardingManager, "manager@test.com");
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "SME Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Tax Clearance Certificate",
            IsRequired = true
        };
        db.ChecklistItems.Add(item);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Kasun Perera",
            Email = "kasun@test.com",
            Role = AppRoles.Reviewer,
            IsActive = true
        };
        db.Users.Add(reviewer);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = reviewer.Id };

        // Act
        var result = await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<ChecklistItemDto>(okResult.Value);
        Assert.Equal(reviewer.Id, updated.AssignedReviewerId);
        Assert.Equal("Kasun Perera", updated.AssignedReviewerName);
    }

    [Fact]
    public async Task AssignReviewer_WithNullReviewerId_ClearsReviewerSuccessfully()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var existingReviewer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Rowan Ellis",
            Email = "rowan@test.com",
            Role = AppRoles.Reviewer,
            IsActive = true
        };
        db.Users.Add(existingReviewer);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Bank Validation Letter",
            AssignedReviewerId = existingReviewer.Id
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = null };

        // Act
        var result = await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<ChecklistItemDto>(okResult.Value);
        Assert.Null(updated.AssignedReviewerId);
        Assert.Null(updated.AssignedReviewerName);

        var inDb = await db.ChecklistItems.FindAsync(item.Id);
        Assert.NotNull(inDb);
        Assert.Null(inDb.AssignedReviewerId);
    }

    [Fact]
    public async Task AssignReviewer_UserWithoutReviewerRole_Returns400BadRequest()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "AML Statement"
        };
        db.ChecklistItems.Add(item);

        var clientUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "John Client",
            Email = "client@test.com",
            Role = AppRoles.Client, // Not a Reviewer
            IsActive = true
        };
        db.Users.Add(clientUser);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = clientUser.Id };

        // Act
        var result = await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("Reviewer", problem.Detail);
    }

    [Fact]
    public async Task AssignReviewer_InactiveUser_Returns400BadRequest()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "KYC Pack"
        };
        db.ChecklistItems.Add(item);

        var inactiveReviewer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Former Staff",
            Email = "staff@test.com",
            Role = AppRoles.Reviewer,
            IsActive = false // Inactive
        };
        db.Users.Add(inactiveReviewer);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = inactiveReviewer.Id };

        // Act
        var result = await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("inactive", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignReviewer_ItemDoesNotBelongToTemplate_Returns404NotFound()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var template1 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template 1" };
        var template2 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template 2" };
        db.ChecklistTemplates.AddRange(template1, template2);

        var itemOnTemplate2 = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template2.Id,
            DocumentName = "Doc 2"
        };
        db.ChecklistItems.Add(itemOnTemplate2);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = null };

        // Act: Requesting itemOnTemplate2 under template1
        var result = await controller.AssignReviewer(template1.Id, itemOnTemplate2.Id, dto);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task AssignReviewer_PersistsAuditLogEntry()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb(AppRoles.Administrator, "compliance-lead@onboardflow.com");
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Partner Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "ISO 27001 Certificate"
        };
        db.ChecklistItems.Add(item);

        var reviewer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Amara Silva",
            Email = "amara@test.com",
            Role = AppRoles.Reviewer,
            IsActive = true
        };
        db.Users.Add(reviewer);
        await db.SaveChangesAsync();

        var dto = new AssignReviewerDto { ReviewerId = reviewer.Id };

        // Act
        await controller.AssignReviewer(template.Id, item.Id, dto);

        // Assert
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "ChecklistItemReviewerAssigned");
        Assert.NotNull(audit);
        Assert.Contains("Amara Silva", audit.Details);
        Assert.Contains("ISO 27001 Certificate", audit.Details);
        Assert.Equal("compliance-lead@onboardflow.com", audit.User);
    }

    [Fact]
    public void AssignReviewer_EndpointRoles_ConfiguredForAdminAndManager()
    {
        var method = typeof(ChecklistItemsController).GetMethod(nameof(ChecklistItemsController.AssignReviewer));
        Assert.NotNull(method);

        var auth = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Contains(AppRoles.Administrator, auth.Roles);
        Assert.Contains(AppRoles.OnboardingManager, auth.Roles);
        Assert.DoesNotContain(AppRoles.Client, auth.Roles);
    }
}
