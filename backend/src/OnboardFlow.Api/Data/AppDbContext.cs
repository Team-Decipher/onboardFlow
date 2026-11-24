using System;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Models;
using OnboardFlow.Api.Modules.Checklists.Domain;
using OnboardFlow.Api.Modules.Onboarding.Domain;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DbHealthCheck> HealthChecks => Set<DbHealthCheck>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientOrganisation> ClientOrganisations => Set<ClientOrganisation>();
    public DbSet<AuditEntry> AuditLogs => Set<AuditEntry>();
    public DbSet<ClientType> ClientTypes => Set<ClientType>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<OnboardingCase> OnboardingCases => Set<OnboardingCase>();
    public DbSet<OnboardingCaseItem> OnboardingCaseItems => Set<OnboardingCaseItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(64);
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            entity.HasOne(u => u.ClientOrganisation)
                  .WithMany(o => o.Users)
                  .HasForeignKey(u => u.ClientOrganisationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ClientOrganisation configuration
        modelBuilder.Entity<ClientOrganisation>(entity =>
        {
            entity.ToTable("ClientOrganisations");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Name).IsRequired().HasMaxLength(256);
            entity.Property(o => o.Category).IsRequired().HasMaxLength(64);
        });

        // AuditEntry configuration
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(128);
            entity.HasIndex(a => a.TimestampUtc);
        });

        // ClientType configuration
        modelBuilder.Entity<ClientType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            entity.HasOne(e => e.DefaultChecklistTemplate)
                  .WithMany(t => t.ClientTypes)
                  .HasForeignKey(e => e.DefaultChecklistTemplateId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ChecklistTemplate configuration
        modelBuilder.Entity<ChecklistTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
        });

        // ChecklistItem configuration
        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.ToTable("ChecklistItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.OrderIndex).HasDefaultValue(1);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            entity.HasOne(e => e.ChecklistTemplate)
                  .WithMany(t => t.Items)
                  .HasForeignKey(e => e.ChecklistTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedReviewer)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedReviewerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ChecklistTemplateId);
            entity.HasIndex(e => new { e.ChecklistTemplateId, e.OrderIndex });
        });

        // OnboardingCase configuration
        modelBuilder.Entity<OnboardingCase>(entity =>
        {
            entity.ToTable("OnboardingCases");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ReferenceCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.ReferenceCode).IsUnique();
            entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
            entity.Property(c => c.CreatedBy).HasMaxLength(100);

            entity.HasOne(c => c.ClientOrganisation)
                  .WithMany()
                  .HasForeignKey(c => c.ClientOrganisationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.ClientType)
                  .WithMany()
                  .HasForeignKey(c => c.ClientTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.OwnerUser)
                  .WithMany()
                  .HasForeignKey(c => c.OwnerUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // OnboardingCaseItem configuration
        modelBuilder.Entity<OnboardingCaseItem>(entity =>
        {
            entity.ToTable("OnboardingCaseItems");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.DocumentName).IsRequired().HasMaxLength(256);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(50);
            entity.Property(i => i.ReviewerName).HasMaxLength(100);
            entity.Property(i => i.UploadedFileName).HasMaxLength(256);

            entity.HasOne(i => i.OnboardingCase)
                  .WithMany(c => c.Items)
                  .HasForeignKey(i => i.OnboardingCaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.AssignedReviewer)
                  .WithMany()
                  .HasForeignKey(i => i.AssignedReviewerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(i => i.OnboardingCaseId);
        });

        // Seed initial templates and client types matching Figma design
        var templateEnterpriseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var templateSmeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var templatePartnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var seedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<ChecklistTemplate>().HasData(
            new ChecklistTemplate
            {
                Id = templateEnterpriseId,
                Name = "Enterprise onboarding",
                Version = "v3",
                Description = "Complete enterprise compliance, financial verification and multi-stage executive signoff.",
                RequiredCount = 6,
                OptionalCount = 2,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            },
            new ChecklistTemplate
            {
                Id = templateSmeId,
                Name = "SME onboarding",
                Version = "v2",
                Description = "A streamlined verification pack designed for small and medium tier operations.",
                RequiredCount = 4,
                OptionalCount = 1,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            },
            new ChecklistTemplate
            {
                Id = templatePartnerId,
                Name = "Partner onboarding",
                Version = "v1",
                Description = "Partner governance agreements and channel accreditation verification.",
                RequiredCount = 5,
                OptionalCount = 1,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            }
        );

        modelBuilder.Entity<ClientType>().HasData(
            new ClientType
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Enterprise",
                Description = "Larger organisations with extended compliance requirements.",
                DefaultChecklistTemplateId = templateEnterpriseId,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            },
            new ClientType
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "SME",
                Description = "A focused checklist for small and medium businesses.",
                DefaultChecklistTemplateId = templateSmeId,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            },
            new ClientType
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Name = "Partner",
                Description = "Agreement-led onboarding for partner organisations.",
                DefaultChecklistTemplateId = templatePartnerId,
                CreatedAtUtc = seedCreatedAt,
                CreatedBy = "System administrator"
            }
        );
    }
}

public class DbHealthCheck
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
