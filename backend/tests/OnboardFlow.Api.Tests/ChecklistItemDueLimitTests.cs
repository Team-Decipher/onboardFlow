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
using Xunit;

namespace OnboardFlow.Api.Tests;

public class ChecklistItemDueLimitTests
{
    private (AppDbContext db, ChecklistItemService service, ChecklistItemsController controller) CreateControllerWithDb(
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

        return (db, service, controller);
    }

    [Fact]
    public async Task SetItemDue_ValidDaysAndFutureDate_Returns200WithComputedValues()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Enterprise Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Director KYC"
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var futureDate = DateTime.UtcNow.AddDays(14).Date;
        var dto = new SetItemDueLimitDto
        {
            DueDays = 14,
            DueDate = futureDate
        };

        // Act
        var result = await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<ChecklistItemDto>(okResult.Value);
        Assert.Equal(14, updated.DueDays);
        Assert.Equal(futureDate, updated.DueDate);
        Assert.False(updated.IsOverdue);

        var inDb = await db.ChecklistItems.FindAsync(item.Id);
        Assert.NotNull(inDb);
        Assert.Equal(14, inDb.DueDays);
        Assert.Equal(futureDate, inDb.DueDate);
    }

    [Fact]
    public async Task SetItemDue_ClearBothLimits_Returns200WithNulls()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Tax Return",
            DueDays = 7,
            DueDate = DateTime.UtcNow.AddDays(7)
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto
        {
            DueDays = null,
            DueDate = null
        };

        // Act
        var result = await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<ChecklistItemDto>(okResult.Value);
        Assert.Null(updated.DueDays);
        Assert.Null(updated.DueDate);
        Assert.False(updated.IsOverdue);

        var inDb = await db.ChecklistItems.FindAsync(item.Id);
        Assert.NotNull(inDb);
        Assert.Null(inDb.DueDays);
        Assert.Null(inDb.DueDate);
    }

    [Fact]
    public async Task SetItemDue_DueDaysLessThan1_Returns400BadRequest()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Audit Report"
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto
        {
            DueDays = 0 // Invalid (< 1)
        };

        // Act
        var result = await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("between 1 and 365", problem.Detail);
    }

    [Fact]
    public async Task SetItemDue_DueDaysGreaterThan365_Returns400BadRequest()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Audit Report"
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto
        {
            DueDays = 400 // Invalid (> 365)
        };

        // Act
        var result = await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("between 1 and 365", problem.Detail);
    }

    [Fact]
    public async Task SetItemDue_PastDueDate_Returns400BadRequest()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Certificate"
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto
        {
            DueDate = DateTime.UtcNow.AddDays(-2) // Past date
        };

        // Act
        var result = await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("past", problem.Detail);
    }

    [Fact]
    public async Task SetItemDue_ItemDoesNotBelongToTemplate_Returns404NotFound()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb();
        var template1 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "T1" };
        var template2 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "T2" };
        db.ChecklistTemplates.AddRange(template1, template2);

        var itemOnT2 = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template2.Id,
            DocumentName = "Doc"
        };
        db.ChecklistItems.Add(itemOnT2);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto { DueDays = 10 };

        // Act: Target itemOnT2 with template1
        var result = await controller.SetItemDue(template1.Id, itemOnT2.Id, dto);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    }

    [Fact]
    public async Task SetItemDue_DispatchesAuditLogEntry()
    {
        // Arrange
        var (db, _, controller) = CreateControllerWithDb(AppRoles.Administrator, "compliance-officer@onboardflow.com");
        var template = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Partner Template" };
        db.ChecklistTemplates.Add(template);

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Proof of Address"
        };
        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();

        var dto = new SetItemDueLimitDto { DueDays = 30 };

        // Act
        await controller.SetItemDue(template.Id, item.Id, dto);

        // Assert
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "ChecklistItemDueUpdated");
        Assert.NotNull(audit);
        Assert.Contains("Proof of Address", audit.Details);
        Assert.Contains("30 days", audit.Details);
        Assert.Equal("compliance-officer@onboardflow.com", audit.User);
    }

    [Fact]
    public void SetItemDue_EndpointRole_RestrictedToAdministrator()
    {
        var method = typeof(ChecklistItemsController).GetMethod(nameof(ChecklistItemsController.SetItemDue));
        Assert.NotNull(method);

        var auth = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Equal(AppRoles.Administrator, auth.Roles);
    }
}
