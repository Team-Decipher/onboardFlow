using Microsoft.EntityFrameworkCore;
using OnboardFlow.Core.Entities;

namespace OnboardFlow.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Case>()
            .HasIndex(c => c.CaseNumber)
            .IsUnique();

        modelBuilder.Entity<Case>()
            .HasMany(c => c.ChecklistItems)
            .WithOne(i => i.Case)
            .HasForeignKey(i => i.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistItem>()
            .HasMany(i => i.Documents)
            .WithOne(d => d.ChecklistItem)
            .HasForeignKey(d => d.ChecklistItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
