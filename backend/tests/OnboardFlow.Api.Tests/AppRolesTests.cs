using OnboardFlow.Api.Common.Authorization;
using Xunit;

namespace OnboardFlow.Api.Tests;

public class AppRolesTests
{
    [Fact]
    public void AppRoles_ContainsAllFourRequiredRoles()
    {
        Assert.Equal(4, AppRoles.All.Count);
        Assert.Contains(AppRoles.Administrator, AppRoles.All);
        Assert.Contains(AppRoles.OnboardingManager, AppRoles.All);
        Assert.Contains(AppRoles.Reviewer, AppRoles.All);
        Assert.Contains(AppRoles.Client, AppRoles.All);
    }

    [Theory]
    [InlineData("Administrator", true)]
    [InlineData("OnboardingManager", true)]
    [InlineData("Reviewer", true)]
    [InlineData("Client", true)]
    [InlineData("administrator", true)] // Case-insensitive
    [InlineData("SuperAdmin", false)]
    [InlineData("Guest", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidRole_ValidatesRolesCorrectly(string? role, bool expectedResult)
    {
        var isValid = AppRoles.IsValidRole(role);
        Assert.Equal(expectedResult, isValid);
    }

    [Theory]
    [InlineData(AppRoles.Client, true)]
    [InlineData("client", true)]
    [InlineData("CLIENT", true)]
    [InlineData(AppRoles.Administrator, false)]
    [InlineData(AppRoles.OnboardingManager, false)]
    [InlineData(AppRoles.Reviewer, false)]
    [InlineData("RandomRole", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void RequiresClientOrganisation_EnforcedOnlyForClientRole(string? role, bool expectedResult)
    {
        var requiresOrg = AppRoles.RequiresClientOrganisation(role);
        Assert.Equal(expectedResult, requiresOrg);
    }
}

