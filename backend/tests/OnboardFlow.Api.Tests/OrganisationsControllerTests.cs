using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Controllers;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Tests;

public class OrganisationsControllerTests
{
    private static (AppDbContext Db, OrganisationsController Controller) CreateController()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        return (db, new OrganisationsController(db));
    }

    [Fact]
    public async Task CreateOrganisation_GeneratesCodeAfterHighestExistingCode()
    {
        var (db, controller) = CreateController();
        db.Organisations.AddRange(
            new ClientOrganisation { ClientCode = "CLI-0015", Name = "Older", ContactEmail = "older@example.com" },
            new ClientOrganisation { ClientCode = "CLI-0018", Name = "Newest", ContactEmail = "newest@example.com" });
        await db.SaveChangesAsync();

        var result = await controller.CreateOrganisation(new CreateOrganisationRequest
        {
            Name = "New organisation",
            PrimaryContact = "Test Contact",
            ContactEmail = "test@example.com"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var organisation = Assert.IsType<ClientOrganisation>(created.Value);
        Assert.Equal("CLI-0019", organisation.ClientCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task CreateOrganisation_RejectsInvalidContactEmail(string email)
    {
        var (_, controller) = CreateController();

        var result = await controller.CreateOrganisation(new CreateOrganisationRequest
        {
            Name = "New organisation",
            PrimaryContact = "Test Contact",
            ContactEmail = email
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task UpdateOrganisation_RejectsInvalidContactEmail(string email)
    {
        var (db, controller) = CreateController();
        var existing = new ClientOrganisation
        {
            ClientCode = "CLI-0018",
            Name = "Existing organisation",
            PrimaryContact = "Existing Contact",
            ContactEmail = "existing@example.com"
        };
        db.Organisations.Add(existing);
        await db.SaveChangesAsync();

        var result = await controller.UpdateOrganisation(existing.Id.ToString(), new UpdateOrganisationRequest
        {
            Name = existing.Name,
            PrimaryContact = existing.PrimaryContact,
            ContactEmail = email
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("existing@example.com", (await db.Organisations.FindAsync(existing.Id))?.ContactEmail);
    }
}
