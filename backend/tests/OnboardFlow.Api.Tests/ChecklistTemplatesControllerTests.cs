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
using OnboardFlow.Api.Modules.Checklists.DTOs;
using OnboardFlow.Api.Modules.Checklists.Services;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class ChecklistTemplatesControllerTests
{
    private (AppDbContext db, ChecklistTemplatesController controller) CreateControllerWithDb(
        string userRole = AppRoles.Administrator, 
        string userEmail = "admin@onboardflow.com")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var service = new ChecklistTemplateService(db);
        var controller = new ChecklistTemplatesController(service);

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
    public async Task GetAll_ReturnsTemplates_WithClientTypeInfo()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var clientType = new ClientType
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            Description = "Enterprise client tier"
        };
        db.ClientTypes.Add(clientType);

        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise Onboarding Template",
            Version = "v1",
            Description = "Standard verification checklist",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ChecklistTemplates.Add(template);

        clientType.DefaultChecklistTemplateId = template.Id;
        template.ClientTypes.Add(clientType);
        await db.SaveChangesAsync();

        // Act
        var actionResult = await controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var templates = Assert.IsAssignableFrom<IEnumerable<ChecklistTemplateDto>>(okResult.Value).ToList();
        Assert.NotEmpty(templates);
        var matching = templates.FirstOrDefault(t => t.Id == template.Id);
        Assert.NotNull(matching);
        Assert.Equal("Enterprise Onboarding Template", matching.Name);
        Assert.Equal(clientType.Id, matching.AssociatedClientTypeId);
        Assert.Equal("Enterprise", matching.AssociatedClientTypeName);
    }

    [Fact]
    public async Task GetAll_WithClientTypeIdFilter_ReturnsOnlyMatchingTemplates()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var clientType1 = new ClientType { Id = Guid.NewGuid(), Name = "Enterprise" };
        var clientType2 = new ClientType { Id = Guid.NewGuid(), Name = "SME" };
        db.ClientTypes.AddRange(clientType1, clientType2);

        var template1 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template 1", Version = "v1" };
        var template2 = new ChecklistTemplate { Id = Guid.NewGuid(), Name = "Template 2", Version = "v1" };
        db.ChecklistTemplates.AddRange(template1, template2);

        clientType1.DefaultChecklistTemplateId = template1.Id;
        template1.ClientTypes.Add(clientType1);

        clientType2.DefaultChecklistTemplateId = template2.Id;
        template2.ClientTypes.Add(clientType2);

        await db.SaveChangesAsync();

        // Act
        var result = await controller.GetAll(clientTypeId: clientType1.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var templates = Assert.IsAssignableFrom<IEnumerable<ChecklistTemplateDto>>(okResult.Value).ToList();
        Assert.Single(templates);
        Assert.Equal(template1.Id, templates[0].Id);
    }

    [Fact]
    public async Task Create_AsAdministrator_ValidRequest_Returns201AndPersistsTemplateAndAudit()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb(AppRoles.Administrator, "admin@test.com");
        var clientType = new ClientType
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            Description = "Corporate tier"
        };
        db.ClientTypes.Add(clientType);
        await db.SaveChangesAsync();

        var dto = new CreateChecklistTemplateDto
        {
            Name = "New Compliance Checklist",
            ClientTypeId = clientType.Id,
            Description = "Checklist for compliance",
            Version = "v2"
        };

        // Act
        var actionResult = await controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(201, createdResult.StatusCode);

        var detail = Assert.IsType<ChecklistTemplateDetailDto>(createdResult.Value);
        Assert.Equal("New Compliance Checklist", detail.Name);
        Assert.Equal("v2", detail.Version);
        Assert.Equal(clientType.Id, detail.AssociatedClientTypeId);
        Assert.Equal("Enterprise", detail.AssociatedClientTypeName);

        // Verify entity persisted in DB
        var persisted = await db.ChecklistTemplates
            .Include(t => t.ClientTypes)
            .FirstOrDefaultAsync(t => t.Id == detail.Id);
        Assert.NotNull(persisted);
        Assert.Equal("New Compliance Checklist", persisted.Name);

        // Verify audit log entry
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "ChecklistTemplateCreated");
        Assert.NotNull(audit);
        Assert.Contains("New Compliance Checklist", audit.Details);
        Assert.Equal("admin@test.com", audit.User);
    }

    [Fact]
    public async Task Create_NonExistentClientType_Returns404NotFound()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var nonExistentId = Guid.NewGuid();

        var dto = new CreateChecklistTemplateDto
        {
            Name = "Sample Checklist",
            ClientTypeId = nonExistentId
        };

        // Act
        var actionResult = await controller.Create(dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Contains(nonExistentId.ToString(), problem.Detail);
    }

    [Fact]
    public async Task Create_DuplicateTemplateNameForSameClientType_Returns409Conflict()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var clientType = new ClientType { Id = Guid.NewGuid(), Name = "SME" };
        db.ClientTypes.Add(clientType);

        var existing = new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            Name = "SME Basic Onboarding",
            Version = "v1"
        };
        db.ChecklistTemplates.Add(existing);
        clientType.DefaultChecklistTemplateId = existing.Id;
        existing.ClientTypes.Add(clientType);
        await db.SaveChangesAsync();

        var dto = new CreateChecklistTemplateDto
        {
            Name = "  sme basic onboarding  ", // Case-insensitive duplicate
            ClientTypeId = clientType.Id
        };

        // Act
        var actionResult = await controller.Create(dto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(conflictResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Contains("already exists", problem.Detail);
    }

    [Fact]
    public async Task Create_InvalidNameLength_Returns400BadRequest()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var clientType = new ClientType { Id = Guid.NewGuid(), Name = "Partner" };
        db.ClientTypes.Add(clientType);
        await db.SaveChangesAsync();

        var dto = new CreateChecklistTemplateDto
        {
            Name = "AB", // Less than 3 characters
            ClientTypeId = clientType.Id
        };

        // Act
        var actionResult = await controller.Create(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public void Controller_EndpointRoles_AreProperlyConfigured()
    {
        // Assert Authorization attributes on endpoints
        var type = typeof(ChecklistTemplatesController);

        var getAllMethod = type.GetMethod(nameof(ChecklistTemplatesController.GetAll));
        Assert.NotNull(getAllMethod);
        var getAllAuth = getAllMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(getAllAuth);
        Assert.Contains(AppRoles.Administrator, getAllAuth.Roles);
        Assert.Contains(AppRoles.OnboardingManager, getAllAuth.Roles);

        var createMethod = type.GetMethod(nameof(ChecklistTemplatesController.Create));
        Assert.NotNull(createMethod);
        var createAuth = createMethod.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(createAuth);
        Assert.Equal(AppRoles.Administrator, createAuth.Roles);
    }
}
