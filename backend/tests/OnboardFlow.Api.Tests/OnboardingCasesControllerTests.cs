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
using OnboardFlow.Api.Modules.Onboarding.DTOs;
using OnboardFlow.Api.Modules.Onboarding.Services;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class OnboardingCasesControllerTests
{
    private (AppDbContext db, OnboardingCasesController controller) CreateControllerWithDb(
        string userRole = AppRoles.OnboardingManager,
        string userEmail = "kasun@example.com",
        string? clientOrgId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var service = new OnboardingCaseService(db);
        var controller = new OnboardingCasesController(service);

        var claims = new List<Claim>
        {
            new("userId", Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, userEmail),
            new("email", userEmail),
            new(ClaimTypes.Name, "Kasun Jayawardena"),
            new(ClaimTypes.Role, userRole),
            new("role", userRole)
        };

        if (!string.IsNullOrEmpty(clientOrgId))
        {
            claims.Add(new Claim("clientOrganisationId", clientOrgId));
        }

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

    private async Task<(ClientOrganisation org, ClientType type, User owner, ChecklistTemplate template)> SeedBasicEntitiesAsync(AppDbContext db)
    {
        var template = new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise onboarding",
            Version = "v3",
            RequiredCount = 2,
            OptionalCount = 0
        };
        db.ChecklistTemplates.Add(template);

        var item1 = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Business registration",
            IsRequired = true,
            DueDays = 5,
            OrderIndex = 1
        };
        var item2 = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = template.Id,
            DocumentName = "Signed agreement",
            IsRequired = true,
            DueDays = 10,
            OrderIndex = 2
        };
        db.ChecklistItems.AddRange(item1, item2);

        var type = new ClientType
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            DefaultChecklistTemplateId = template.Id
        };
        db.ClientTypes.Add(type);

        var org = new ClientOrganisation
        {
            Id = Guid.NewGuid(),
            Name = "Ceylon Exports (Pvt) Ltd",
            Category = "Enterprise",
            ContactName = "Nimali Perera",
            ContactEmail = "nimali@example.com"
        };
        db.ClientOrganisations.Add(org);

        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Kasun Jayawardena",
            Email = "kasun@example.com",
            Role = AppRoles.OnboardingManager,
            IsActive = true
        };
        db.Users.Add(owner);

        await db.SaveChangesAsync();
        return (org, type, owner, template);
    }

    [Fact]
    public async Task CreateCase_WhenValid_InstantiatesChecklistItemsAndReturns201()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var (org, type, owner, _) = await SeedBasicEntitiesAsync(db);

        var request = new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = org.Id,
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(30),
            Status = "In review"
        };

        // Act
        var actionResult = await controller.CreateCase(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(201, createdResult.StatusCode);

        var dto = Assert.IsType<OnboardingCaseDetailDto>(createdResult.Value);
        Assert.StartsWith("ONB-", dto.ReferenceCode);
        Assert.Equal("Ceylon Exports (Pvt) Ltd", dto.ClientOrganisation.Name);
        Assert.Equal(2, dto.Items.Count);
        Assert.Contains(dto.Items, i => i.DocumentName == "Business registration");
        Assert.Contains(dto.Items, i => i.DocumentName == "Signed agreement");

        // Verify audit log
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.TargetEntity == "OnboardingCase");
        Assert.NotNull(audit);
        Assert.Equal("ONBOARDING_CASE_CREATED", audit.Action);
    }

    [Fact]
    public async Task CreateCase_WhenRequiredFieldsMissing_Returns400()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();

        var request = new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = Guid.Empty,
            OwnerUserId = Guid.Empty,
            TargetCompletionDate = default
        };

        // Act
        var actionResult = await controller.CreateCase(request);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objResult.StatusCode);
    }

    [Fact]
    public async Task CreateCase_WhenClientOrganisationNotFound_Returns404()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var (_, type, owner, _) = await SeedBasicEntitiesAsync(db);

        var request = new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = Guid.NewGuid(), // Non-existent
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(15)
        };

        // Act
        var actionResult = await controller.CreateCase(request);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status404NotFound, objResult.StatusCode);
    }

    [Fact]
    public async Task GetCases_ReturnsListWithPaginationAndCounts()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var (org, type, owner, _) = await SeedBasicEntitiesAsync(db);

        // Create 2 cases
        await controller.CreateCase(new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = org.Id,
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(15),
            Status = "In review"
        });

        await controller.CreateCase(new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = org.Id,
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(10),
            Status = "Approved"
        });

        // Act
        var result = await controller.GetCases(search: null, status: null, owner: null, type: null, page: 1, pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<OnboardingCaseListResponse>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.Counts.All);
        Assert.Equal(1, response.Counts.InProgress);
        Assert.Equal(1, response.Counts.Completed);
    }

    [Fact]
    public async Task GetCaseById_WhenFound_Returns200WithDetails()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var (org, type, owner, _) = await SeedBasicEntitiesAsync(db);

        var created = await controller.CreateCase(new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = org.Id,
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(20),
            Status = "In review"
        });
        var createdDto = Assert.IsType<OnboardingCaseDetailDto>(((CreatedAtActionResult)created.Result!).Value);

        // Act
        var result = await controller.GetCaseById(createdDto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OnboardingCaseDetailDto>(okResult.Value);
        Assert.Equal(createdDto.Id, dto.Id);
        Assert.Equal(createdDto.ReferenceCode, dto.ReferenceCode);
        Assert.NotEmpty(dto.Timeline);
        Assert.NotEmpty(dto.Activities);
    }

    [Fact]
    public async Task GetCaseById_WhenNotFound_Returns404()
    {
        // Arrange
        var (_, controller) = CreateControllerWithDb();

        // Act
        var result = await controller.GetCaseById(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCase_WhenValid_UpdatesFieldsAndReturns200()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        var (org, type, owner, _) = await SeedBasicEntitiesAsync(db);

        var created = await controller.CreateCase(new CreateOnboardingCaseRequest
        {
            ClientOrganisationId = org.Id,
            ClientTypeId = type.Id,
            OwnerUserId = owner.Id,
            TargetCompletionDate = DateTime.UtcNow.AddDays(20),
            Status = "In review"
        });
        var createdDto = Assert.IsType<OnboardingCaseDetailDto>(((CreatedAtActionResult)created.Result!).Value);

        var updateReq = new UpdateOnboardingCaseRequest
        {
            Status = "Approved",
            TargetCompletionDate = DateTime.UtcNow.AddDays(40)
        };

        // Act
        var updateResult = await controller.UpdateCase(createdDto.Id, updateReq);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updatedDto = Assert.IsType<OnboardingCaseDetailDto>(okResult.Value);
        Assert.Equal("Approved", updatedDto.Status);

        // Verify update audit log
        var updateAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "ONBOARDING_CASE_UPDATED");
        Assert.NotNull(updateAudit);
    }

    [Fact]
    public async Task GetFormOptions_ReturnsOrganisationsClientTypesAndOwners()
    {
        // Arrange
        var (db, controller) = CreateControllerWithDb();
        await SeedBasicEntitiesAsync(db);

        // Act
        var result = await controller.GetFormOptions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var options = Assert.IsType<CaseFormOptionsDto>(okResult.Value);
        Assert.NotEmpty(options.Organisations);
        Assert.NotEmpty(options.ClientTypes);
        Assert.NotEmpty(options.Owners);
    }

    [Fact]
    public void EndpointAuthorization_PostAndPut_RequireAdministratorOrOnboardingManager()
    {
        var postMethod = typeof(OnboardingCasesController).GetMethod(nameof(OnboardingCasesController.CreateCase));
        var postAttr = postMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(postAttr);
        Assert.Contains(AppRoles.Administrator, postAttr.Roles);
        Assert.Contains(AppRoles.OnboardingManager, postAttr.Roles);

        var putMethod = typeof(OnboardingCasesController).GetMethod(nameof(OnboardingCasesController.UpdateCase));
        var putAttr = putMethod?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(putAttr);
        Assert.Contains(AppRoles.Administrator, putAttr.Roles);
        Assert.Contains(AppRoles.OnboardingManager, putAttr.Roles);
    }
}
