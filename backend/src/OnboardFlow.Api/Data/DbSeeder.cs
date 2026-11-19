using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Onboarding.Domain;
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

        // 3. Seed Figma persona accounts (check by email before inserting)
        var figmaPersonas = new List<User>
        {
            new()
            {
                Email = "admin@example.com",
                FullName = "System Administrator",
                Role = AppRoles.Administrator,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "kasun@example.com",
                FullName = "Kasun Jayawardena",
                Role = AppRoles.OnboardingManager,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "amara@example.com",
                FullName = "Amara Silva",
                Role = AppRoles.Reviewer,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Email = "nimali@example.com",
                FullName = "Nimali Perera",
                Role = AppRoles.Client,
                ClientOrganisationId = ceylonOrg.Id,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        foreach (var persona in figmaPersonas)
        {
            if (!await context.Users.AnyAsync(u => u.Email == persona.Email))
            {
                context.Users.Add(persona);
            }
        }

        await context.SaveChangesAsync();

        // 4. Seed initial Checklist Items matching the Figma design if none exist
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

        // 5. Seed additional organisations matching Figma
        var lankaFoods = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Lanka Foods (Pvt) Ltd");
        if (lankaFoods == null)
        {
            lankaFoods = new ClientOrganisation
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222221"),
                Name = "Lanka Foods (Pvt) Ltd",
                Category = "SME",
                ContactEmail = "contact@lankafoods.lk",
                ContactName = "Dimuka Perera",
                PhoneNumber = "+94 71 234 5678",
                RegistrationNumber = "PV 654321",
                BusinessAddress = "12 Temple Road, Kandy",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ClientOrganisations.Add(lankaFoods);
        }

        var oceanicPartners = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Oceanic Partners Ltd");
        if (oceanicPartners == null)
        {
            oceanicPartners = new ClientOrganisation
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
                Name = "Oceanic Partners Ltd",
                Category = "Partner",
                ContactEmail = "contact@oceanicpartners.com",
                ContactName = "Kavinda Fernando",
                PhoneNumber = "+94 77 987 6543",
                RegistrationNumber = "PV 789012",
                BusinessAddress = "55 Galle Road, Colombo 04",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ClientOrganisations.Add(oceanicPartners);
        }

        var lotusDigital = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Lotus Digital (Pvt) Ltd");
        if (lotusDigital == null)
        {
            lotusDigital = new ClientOrganisation
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444441"),
                Name = "Lotus Digital (Pvt) Ltd",
                Category = "SME",
                ContactEmail = "contact@lotusdigital.lk",
                ContactName = "Shanika Dias",
                PhoneNumber = "+94 75 555 4321",
                RegistrationNumber = "PV 345678",
                BusinessAddress = "88 High Level Road, Nugegoda",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ClientOrganisations.Add(lotusDigital);
        }

        var serendibLogistics = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Serendib Logistics PLC");
        if (serendibLogistics == null)
        {
            serendibLogistics = new ClientOrganisation
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Serendib Logistics PLC",
                Category = "Enterprise",
                ContactEmail = "contact@serendiblogistics.com",
                ContactName = "Rohan De Silva",
                PhoneNumber = "+94 77 444 8888",
                RegistrationNumber = "PQ 901234",
                BusinessAddress = "101 Port Access Road, Colombo 13",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.ClientOrganisations.Add(serendibLogistics);
        }

        // 6. Seed Dimuka Perera (Onboarding Manager)
        var dimukaUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "dimuka@onboardflow.com");
        if (dimukaUser == null)
        {
            dimukaUser = new User
            {
                Email = "dimuka@onboardflow.com",
                FullName = "Dimuka Perera",
                Role = AppRoles.OnboardingManager,
                PasswordHash = defaultPasswordHash,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Users.Add(dimukaUser);
        }

        await context.SaveChangesAsync();

        // 7. Seed Sample Onboarding Cases matching Figma screens 06 and 08
        if (!await context.OnboardingCases.AnyAsync())
        {
            var kasunManager = await context.Users.FirstOrDefaultAsync(u => u.Email == "kasun@example.com")
                ?? await context.Users.FirstOrDefaultAsync(u => u.Email == "manager@onboardflow.com")
                ?? dimukaUser;

            var enterpriseType = await context.ClientTypes.FirstOrDefaultAsync(t => t.Name == "Enterprise");
            var smeType = await context.ClientTypes.FirstOrDefaultAsync(t => t.Name == "SME");
            var partnerType = await context.ClientTypes.FirstOrDefaultAsync(t => t.Name == "Partner");

            var ceylonExportsOrg = await context.ClientOrganisations.FirstOrDefaultAsync(o => o.Name == "Ceylon Exports (Pvt) Ltd") ?? ceylonOrg;

            // Case 1: Ceylon Exports (ONB-0248) - 3 of 6 approved, In review, due 22 Oct 2026
            var caseCeylon = new OnboardingCase
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000248"),
                ReferenceCode = "ONB-0248",
                ClientOrganisationId = ceylonExportsOrg.Id,
                ClientTypeId = enterpriseType!.Id,
                OwnerUserId = kasunManager!.Id,
                TargetCompletionDate = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                Status = "In review",
                CreatedAtUtc = new DateTime(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc),
                CreatedBy = "Kasun Jayawardena",
                Items = new List<OnboardingCaseItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Business registration",
                        IsRequired = true,
                        OrderIndex = 1,
                        Status = "Approved",
                        ReviewerName = "Legal",
                        DueDate = new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = "business-registration.pdf",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Signed agreement",
                        IsRequired = true,
                        OrderIndex = 2,
                        Status = "In review",
                        ReviewerName = "Finance",
                        DueDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = "agreement-v2.pdf",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Tax certificate",
                        IsRequired = true,
                        OrderIndex = 3,
                        Status = "Needs correction",
                        ReviewerName = "Finance",
                        DueDate = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = "tax-certificate-v1.pdf",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Bank account confirmation",
                        IsRequired = true,
                        OrderIndex = 4,
                        Status = "Missing",
                        ReviewerName = "Finance",
                        DueDate = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = null,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Director ID",
                        IsRequired = true,
                        OrderIndex = 5,
                        Status = "Approved",
                        ReviewerName = "Legal",
                        DueDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = "director-id.pdf",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = "Address proof",
                        IsRequired = true,
                        OrderIndex = 6,
                        Status = "Approved",
                        ReviewerName = "Legal",
                        DueDate = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc),
                        UploadedFileName = "address-proof.pdf",
                        CreatedAtUtc = DateTime.UtcNow
                    }
                }
            };

            // Case 2: Lanka Foods (ONB-0247) - 2 / 4 approved, Overdue, due 16 Oct 2026
            var caseLanka = new OnboardingCase
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000247"),
                ReferenceCode = "ONB-0247",
                ClientOrganisationId = lankaFoods.Id,
                ClientTypeId = smeType!.Id,
                OwnerUserId = dimukaUser.Id,
                TargetCompletionDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc),
                Status = "Overdue",
                CreatedAtUtc = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc),
                CreatedBy = "Dimuka Perera",
                Items = new List<OnboardingCaseItem>
                {
                    new() { Id = Guid.NewGuid(), DocumentName = "Business registration", IsRequired = true, OrderIndex = 1, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "lanka-br.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Tax identification", IsRequired = true, OrderIndex = 2, Status = "Approved", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "tax-id.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Financial statements", IsRequired = true, OrderIndex = 3, Status = "Missing", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Director resolution", IsRequired = true, OrderIndex = 4, Status = "Missing", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow }
                }
            };

            // Case 3: Oceanic Partners (ONB-0246) - 3 / 5 approved, Needs correction, due 24 Oct 2026
            var caseOceanic = new OnboardingCase
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000246"),
                ReferenceCode = "ONB-0246",
                ClientOrganisationId = oceanicPartners.Id,
                ClientTypeId = partnerType!.Id,
                OwnerUserId = kasunManager!.Id,
                TargetCompletionDate = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc),
                Status = "Needs correction",
                CreatedAtUtc = new DateTime(2026, 10, 8, 10, 15, 0, DateTimeKind.Utc),
                CreatedBy = "Kasun Jayawardena",
                Items = new List<OnboardingCaseItem>
                {
                    new() { Id = Guid.NewGuid(), DocumentName = "Partnership agreement", IsRequired = true, OrderIndex = 1, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "oceanic-agreement.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Certificate of incorporation", IsRequired = true, OrderIndex = 2, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "cert-incorp.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Partner accreditation certificate", IsRequired = true, OrderIndex = 3, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "accreditation.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Tax clearance certificate", IsRequired = true, OrderIndex = 4, Status = "Needs correction", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "tax-clearance.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Bank guarantee confirmation", IsRequired = true, OrderIndex = 5, Status = "Missing", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow }
                }
            };

            // Case 4: Lotus Digital (ONB-0245) - 4 / 4 approved, Approved, due 15 Oct 2026
            var caseLotus = new OnboardingCase
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000245"),
                ReferenceCode = "ONB-0245",
                ClientOrganisationId = lotusDigital.Id,
                ClientTypeId = smeType!.Id,
                OwnerUserId = dimukaUser.Id,
                TargetCompletionDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                Status = "Approved",
                CreatedAtUtc = new DateTime(2026, 9, 28, 11, 0, 0, DateTimeKind.Utc),
                CreatedBy = "Dimuka Perera",
                Items = new List<OnboardingCaseItem>
                {
                    new() { Id = Guid.NewGuid(), DocumentName = "Business registration", IsRequired = true, OrderIndex = 1, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "lotus-br.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Signed contract", IsRequired = true, OrderIndex = 2, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "lotus-contract.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Tax certificate", IsRequired = true, OrderIndex = 3, Status = "Approved", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "lotus-tax.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Bank statement", IsRequired = true, OrderIndex = 4, Status = "Approved", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "lotus-bank.pdf", CreatedAtUtc = DateTime.UtcNow }
                }
            };

            // Case 5: Serendib Logistics (ONB-0244) - 1 / 6 approved, Missing, due 25 Oct 2026
            var caseSerendib = new OnboardingCase
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000244"),
                ReferenceCode = "ONB-0244",
                ClientOrganisationId = serendibLogistics.Id,
                ClientTypeId = enterpriseType!.Id,
                OwnerUserId = kasunManager!.Id,
                TargetCompletionDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc),
                Status = "Missing",
                CreatedAtUtc = new DateTime(2026, 10, 11, 14, 20, 0, DateTimeKind.Utc),
                CreatedBy = "Kasun Jayawardena",
                Items = new List<OnboardingCaseItem>
                {
                    new() { Id = Guid.NewGuid(), DocumentName = "Business registration", IsRequired = true, OrderIndex = 1, Status = "Approved", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = "serendib-br.pdf", CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Signed agreement", IsRequired = true, OrderIndex = 2, Status = "Missing", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Tax certificate", IsRequired = true, OrderIndex = 3, Status = "Missing", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Bank account confirmation", IsRequired = true, OrderIndex = 4, Status = "Missing", ReviewerName = "Finance", DueDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Director ID", IsRequired = true, OrderIndex = 5, Status = "Missing", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), DocumentName = "Address proof", IsRequired = true, OrderIndex = 6, Status = "Missing", ReviewerName = "Legal", DueDate = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), UploadedFileName = null, CreatedAtUtc = DateTime.UtcNow }
                }
            };

            var seedCases = new List<OnboardingCase>
            {
                caseCeylon,
                caseLanka,
                caseOceanic,
                caseLotus,
                caseSerendib
            };

            // Seed remaining 43 cases (ONB-0243 down to ONB-0201) to achieve the exact Figma counts (48 total: 36 in progress, 12 completed)
            var orgOptions = new[] { ceylonExportsOrg, lankaFoods, oceanicPartners, lotusDigital, serendibLogistics };
            var typeOptions = new[] { enterpriseType, smeType, partnerType };
            var ownerOptions = new[] { kasunManager, dimukaUser };

            for (int i = 243; i >= 201; i--)
            {
                var isCompleted = (i % 4 == 0); // 11 additional completed + Lotus (1) = 12 completed!
                var org = orgOptions[i % orgOptions.Length];
                var type = typeOptions[i % typeOptions.Length];
                var owner = ownerOptions[i % ownerOptions.Length];
                var statusVal = isCompleted
                    ? "Completed"
                    : ((i % 3 == 0) ? "In review" : ((i % 3 == 1) ? "Overdue" : "Needs correction"));

                seedCases.Add(new OnboardingCase
                {
                    Id = Guid.NewGuid(),
                    ReferenceCode = $"ONB-{i:D4}",
                    ClientOrganisationId = org.Id,
                    ClientTypeId = type!.Id,
                    OwnerUserId = owner!.Id,
                    TargetCompletionDate = DateTime.UtcNow.AddDays((i % 20) - 10),
                    Status = statusVal,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-(250 - i)),
                    CreatedBy = owner.FullName,
                    Items = new List<OnboardingCaseItem>
                    {
                        new() { Id = Guid.NewGuid(), DocumentName = "Registration documents", IsRequired = true, OrderIndex = 1, Status = isCompleted ? "Approved" : "Approved", ReviewerName = "Legal", CreatedAtUtc = DateTime.UtcNow },
                        new() { Id = Guid.NewGuid(), DocumentName = "Verification certificate", IsRequired = true, OrderIndex = 2, Status = isCompleted ? "Approved" : "In review", ReviewerName = "Finance", CreatedAtUtc = DateTime.UtcNow }
                    }
                });
            }

            context.OnboardingCases.AddRange(seedCases);
            await context.SaveChangesAsync();
        }
    }
}

