using System;
using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnboardFlow.Api.Data;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Cases.Repositories;
using OnboardFlow.Api.Modules.Cases.Services;
using OnboardFlow.Api.Modules.Checklists.Repositories;
using OnboardFlow.Api.Modules.Checklists.Services;
using OnboardFlow.Api.Modules.Documents.Application;
using OnboardFlow.Api.Modules.Documents.Application.Storage;
using OnboardFlow.Api.Modules.Documents.Application.Validation;
using OnboardFlow.Api.Modules.Documents.Infrastructure;
using OnboardFlow.Api.Modules.Onboarding.Services;
using OnboardFlow.Api.Modules.UsersAndAccess.Authorization;
using OnboardFlow.Api.Modules.UsersAndAccess.Services;
using Organisation = OnboardFlow.Api.Models.ClientOrganisation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IChecklistItemRepository, ChecklistItemRepository>();
builder.Services.AddScoped<IChecklistItemService, ChecklistItemService>();
builder.Services.AddScoped<IChecklistTemplateService, ChecklistTemplateService>();
builder.Services.AddScoped<IPasswordHasher, Sha256PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOnboardingCaseService, OnboardingCaseService>();
builder.Services.AddScoped<ICaseRepository, CaseRepository>();
builder.Services.AddScoped<ICaseQueryService, CaseQueryService>();

// Documents module options (validated on startup)
builder.Services.AddOptions<DocumentsOptions>()
    .Bind(builder.Configuration.GetSection(DocumentsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureStorageOptions>()
    .Bind(builder.Configuration.GetSection(AzureStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var storageOpts = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
    if (string.IsNullOrWhiteSpace(storageOpts.ConnectionString))
    {
        throw new InvalidOperationException(
            "AzureStorage:ConnectionString is not configured. Set it via user-secrets, AzureStorage__ConnectionString, or appsettings.Development.json.");
    }
    return new BlobServiceClient(storageOpts.ConnectionString);
});

builder.Services.AddScoped<IFileValidator, PassThroughFileValidator>();
builder.Services.AddScoped<IFileStorage, AzureBlobFileStorage>();

// Database connection
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddDbContext<DocumentsDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "documents")));
}

// CORS setup
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- JWT Authentication Configuration ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-change-me-use-at-least-32-characters";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OnboardFlow";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OnboardFlowClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetService<AppDbContext>();
    if (db != null)
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }

    var documentsDb = services.GetService<DocumentsDbContext>();
    if (documentsDb != null)
    {
        try
        {
            documentsDb.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while migrating the documents database schema.");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        try
        {
            var blobService = services.GetService<BlobServiceClient>();
            var storageOpts = services.GetService<IOptions<AzureStorageOptions>>()?.Value;
            if (blobService != null && !string.IsNullOrWhiteSpace(storageOpts?.ContainerName))
            {
                await blobService
                    .GetBlobContainerClient(storageOpts.ContainerName)
                    .CreateIfNotExistsAsync(PublicAccessType.None);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not ensure Azure Blob container exists on startup.");
        }
    }

    if (db != null)
    {

        // Seed Client Organisations matching Screen 10 & 11 mockups
        if (!db.Organisations.Any())
        {
            var ceylonId = Guid.NewGuid();
            var lankaId = Guid.NewGuid();
            var oceanicId = Guid.NewGuid();
            var lotusId = Guid.NewGuid();

            var orgs = new List<Organisation>
            {
                new()
                {
                    Id = ceylonId,
                    ClientCode = "CLI-0018",
                    Name = "Ceylon Exports (Pvt) Ltd",
                    City = "Colombo",
                    ClientType = "Enterprise",
                    PrimaryContact = "Nimali Perera",
                    ContactEmail = "nimali@example.com",
                    PhoneNumber = "+94 11 234 5678",
                    BusinessRegistrationNumber = "PV 123456",
                    BusinessAddress = "42 Marine Drive, Colombo 03",
                    ActiveCasesCount = 1,
                    CasesSummary = "1 active",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-3),
                    LinkedCases = new List<LinkedCase>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrganisationId = ceylonId,
                            CaseNumber = "OBF-2026-018",
                            Title = "Enterprise Statutory KYC & Compliance Onboarding",
                            Status = "Active",
                            ProgressPercentage = 67,
                            ReviewerName = "Kasun Jayawardena",
                            TargetCompletionDate = DateTime.UtcNow.AddDays(10),
                            CreatedAtUtc = DateTime.UtcNow.AddDays(-20)
                        }
                    }
                },
                new()
                {
                    Id = lankaId,
                    ClientCode = "CLI-0017",
                    Name = "Lanka Foods",
                    City = "Kandy",
                    ClientType = "SME",
                    PrimaryContact = "Dilan Fernando",
                    ContactEmail = "dilan@lankafoods.lk",
                    PhoneNumber = "+94 81 223 4567",
                    BusinessRegistrationNumber = "PV 987654",
                    BusinessAddress = "15 Peradeniya Road, Kandy",
                    ActiveCasesCount = 2,
                    CasesSummary = "2 active",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-4),
                    LinkedCases = new List<LinkedCase>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrganisationId = lankaId,
                            CaseNumber = "OBF-2026-017A",
                            Title = "SME Fast-Track Supplier Documentation",
                            Status = "Active",
                            ProgressPercentage = 45,
                            ReviewerName = "Amara Silva",
                            TargetCompletionDate = DateTime.UtcNow.AddDays(5),
                            CreatedAtUtc = DateTime.UtcNow.AddDays(-15)
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrganisationId = lankaId,
                            CaseNumber = "OBF-2026-017B",
                            Title = "Export Quality & Standard Certification Verification",
                            Status = "Active",
                            ProgressPercentage = 80,
                            ReviewerName = "Kasun Jayawardena",
                            TargetCompletionDate = DateTime.UtcNow.AddDays(18),
                            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                        }
                    }
                },
                new()
                {
                    Id = oceanicId,
                    ClientCode = "CLI-0016",
                    Name = "Oceanic Partners",
                    City = "Galle",
                    ClientType = "Partner",
                    PrimaryContact = "Shanika de Silva",
                    ContactEmail = "shanika@oceanicpartners.lk",
                    PhoneNumber = "+94 91 334 5678",
                    BusinessRegistrationNumber = "PV 456123",
                    BusinessAddress = "88 Fort Light St, Galle",
                    ActiveCasesCount = 1,
                    CasesSummary = "1 active",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-6),
                    LinkedCases = new List<LinkedCase>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrganisationId = oceanicId,
                            CaseNumber = "OBF-2026-016",
                            Title = "Channel Partner Legal Agreement & NDA Vetting",
                            Status = "Active",
                            ProgressPercentage = 50,
                            ReviewerName = "Amara Silva",
                            TargetCompletionDate = DateTime.UtcNow.AddDays(7),
                            CreatedAtUtc = DateTime.UtcNow.AddDays(-12)
                        }
                    }
                },
                new()
                {
                    Id = lotusId,
                    ClientCode = "CLI-0015",
                    Name = "Lotus Digital",
                    City = "Colombo",
                    ClientType = "SME",
                    PrimaryContact = "Ruwan Jayasuriya",
                    ContactEmail = "ruwan@lotusdigital.lk",
                    PhoneNumber = "+94 11 889 0123",
                    BusinessRegistrationNumber = "PV 321654",
                    BusinessAddress = "120 Galle Road, Colombo 03",
                    ActiveCasesCount = 0,
                    CasesSummary = "Completed",
                    Status = "Active",
                    CreatedAtUtc = DateTime.UtcNow.AddMonths(-8),
                    LinkedCases = new List<LinkedCase>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrganisationId = lotusId,
                            CaseNumber = "OBF-2026-015",
                            Title = "Digital Service Provider Compliance Review",
                            Status = "Completed",
                            ProgressPercentage = 100,
                            ReviewerName = "Ravindu Fernando",
                            TargetCompletionDate = DateTime.UtcNow.AddDays(-30),
                            CreatedAtUtc = DateTime.UtcNow.AddDays(-60)
                        }
                    }
                }
            };

            db.Organisations.AddRange(orgs);
            db.SaveChanges();
            Console.WriteLine("[Database] Seeded initial client organisations successfully.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "OnboardFlow API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Authentication MUST be before Authorization!
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>(); // 🔒 Rejects deactivated users on all authenticated requests
app.UseAuthorization();

app.MapControllers();

// Seed initial database records
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(dbContext);
}

app.Run();
