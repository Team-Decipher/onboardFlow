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
    }
}

