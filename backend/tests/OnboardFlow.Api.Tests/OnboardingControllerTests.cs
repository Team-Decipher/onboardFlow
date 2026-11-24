using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Controllers;
using OnboardFlow.Api.Models;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class OnboardingControllerTests
{
    private OnboardingController CreateControllerWithUser(string role, string email, string? clientOrgId = null)
    {
        var controller = new OnboardingController();
        var claims = new List<Claim>
        {
            new("userId", Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, email),
            new("email", email),
            new(ClaimTypes.Role, role),
            new("role", role)
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

        return controller;
    }

    [Fact]
    public void GetWorkflow_WhenClientBelongsToSameOrganisation_Returns200Ok()
    {
        // Arrange
        var controller = CreateControllerWithUser(
            role: AppRoles.Client,
            email: "client@example.com",
            clientOrgId: OnboardingController.CaseClientOrganisationId.ToString()
        );

        // Act
        var result = controller.GetWorkflow();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var workflow = Assert.IsType<OnboardingWorkflowResponse>(okResult.Value);
        Assert.Equal("OBF-2026-904", workflow.CaseId);
    }

    [Fact]
    public void GetWorkflow_WhenClientBelongsToDifferentOrganisation_Returns403Forbidden()
    {
        // Arrange
        var differentOrgId = Guid.NewGuid().ToString();
        var controller = CreateControllerWithUser(
            role: AppRoles.Client,
            email: "other@client.com",
            clientOrgId: differentOrgId
        );

        // Act
        var result = controller.GetWorkflow();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void ToggleStep_WhenAssignedReviewer_Returns200Ok()
    {
        // Arrange
        var controller = CreateControllerWithUser(
            role: AppRoles.Reviewer,
            email: OnboardingController.AssignedReviewerEmail
        );

        // Act
        var result = controller.ToggleStep("step-3");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var workflow = Assert.IsType<OnboardingWorkflowResponse>(okResult.Value);
        Assert.NotNull(workflow);
    }

    [Fact]
    public void ToggleStep_WhenUnassignedReviewer_Returns403Forbidden()
    {
        // Arrange
        var controller = CreateControllerWithUser(
            role: AppRoles.Reviewer,
            email: "unassigned-reviewer@onboardflow.com"
        );

        // Act
        var result = controller.ToggleStep("step-3");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void ToggleStep_WhenAdministrator_Returns200Ok()
    {
        // Arrange
        var controller = CreateControllerWithUser(
            role: AppRoles.Administrator,
            email: "admin@onboardflow.com"
        );

        // Act
        var result = controller.ToggleStep("step-1");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var workflow = Assert.IsType<OnboardingWorkflowResponse>(okResult.Value);
        Assert.NotNull(workflow);
    }
}

