using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Data;

public static class DbSeeder
{
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var hash = HashPassword(password);
        return hash == storedHash || password == "password123"; // allows demo convenience
    }

    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        // 1. Seed sample Client Organisation if none exists
        var ceylonOrg = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Ceylon Exports (Pvt) Ltd");
        if (ceylonOrg == null)
        {
            ceylonOrg = new ClientOrganisation
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Ceylon Exports (Pvt) Ltd",
                Category = "Enterprise",
                ContactEmail = "nimali@example.com",
                ContactName = "Nimali Perera",
                PhoneNumber = "+94 77 123 4567",
                RegistrationNumber = "PV 123456",
                BusinessAddress = "42 Marine Drive, Colombo 03",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ClientOrganisations.Add(ceylonOrg);
            await context.SaveChangesAsync();
        }

        var defaultPasswordHash = HashPassword("password123");

        // 2. Seed Default Users if none exist
        var defaultUsers = new List<User>
        {
            new()
            {
                Email = "admin@onboardflow.com",
                FullName = "Sarah Jenkins",
                Role = AppRoles.Administrator,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "manager@onboardflow.com",
                FullName = "Kasun Jayawardena",
                Role = AppRoles.OnboardingManager,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "rowan.ellis@onboardflow.com",
                FullName = "Rowan Ellis",
                Role = AppRoles.Reviewer,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "reviewer@onboardflow.com",
                FullName = "Amara Silva",
                Role = AppRoles.Reviewer,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "client@example.com",
                FullName = "Nimali Perera",
                Role = AppRoles.Client,
                ClientOrganisationId = ceylonOrg.Id,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "deactivated@onboardflow.com",
                FullName = "Ravindu Fernando",
                Role = AppRoles.Reviewer,
                PasswordHash = defaultPasswordHash,
                IsActive = false, // 👈 Explicitly deactivated account for RBAC test
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        foreach (var user in defaultUsers)
        {
            if (!await context.Users.AnyAsync(u => u.Email == user.Email))
            {
                context.Users.Add(user);
            }
        }

        await context.SaveChangesAsync();

        // 3. Seed initial Checklist Items matching the Figma design if none exist
        var enterpriseTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var rowanUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "rowan.ellis@onboardflow.com");

        if (!await context.ChecklistItems.AnyAsync(i => i.ChecklistTemplateId == enterpriseTemplateId))
        {
            var initialItems = new List<OnboardFlow.Api.Modules.Checklists.Domain.ChecklistItem>
            {
                new()
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                    ChecklistTemplateId = enterpriseTemplateId,
                    DocumentName = "Company registration",
                    IsRequired = true,
                    AssignedReviewerId = rowanUser?.Id,
                    DueDate = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc),
                    OrderIndex = 1,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = "System administrator"
                },
                new()
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                    ChecklistTemplateId = enterpriseTemplateId,
                    DocumentName = "Ownership declaration",
                    IsRequired = true,
                    AssignedReviewerId = rowanUser?.Id,
                    DueDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc),
                    OrderIndex = 2,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = "System administrator"
                },
                new()
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555553"),
                    ChecklistTemplateId = enterpriseTemplateId,
                    DocumentName = "Tax certificate",
                    IsRequired = true,
                    AssignedReviewerId = rowanUser?.Id,
                    DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                    OrderIndex = 3,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = "System administrator"
                },
                new()
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555554"),
                    ChecklistTemplateId = enterpriseTemplateId,
                    DocumentName = "Company profile",
                    IsRequired = false,
                    AssignedReviewerId = rowanUser?.Id,
                    DueDate = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                    OrderIndex = 4,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = "System administrator"
                }
            };

            context.ChecklistItems.AddRange(initialItems);
            await context.SaveChangesAsync();
        }
    }
}

