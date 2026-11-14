using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.UsersAndAccess.Domain;

namespace OnboardFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DbHealthCheck> HealthChecks => Set<DbHealthCheck>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientOrganisation> ClientOrganisations => Set<ClientOrganisation>();
    public DbSet<AuditEntry> AuditLogs => Set<AuditEntry>();

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
    }
}

public class DbHealthCheck
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
