using System;
using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DbHealthCheck> HealthChecks => Set<DbHealthCheck>();
    public DbSet<ClientType> ClientTypes => Set<ClientType>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<ChecklistTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
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
