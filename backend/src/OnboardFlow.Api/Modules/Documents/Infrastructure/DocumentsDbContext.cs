using Microsoft.EntityFrameworkCore;
using OnboardFlow.Api.Modules.Documents.Domain;

namespace OnboardFlow.Api.Modules.Documents.Infrastructure;

/// <summary>
/// Dedicated EF Core DbContext for the Documents module, isolated in PostgreSQL schema 'documents'.
/// </summary>
public class DocumentsDbContext : DbContext
{
    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("documents");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id).HasColumnName("id");
            entity.Property(d => d.CaseId).HasColumnName("case_id").IsRequired();
            entity.Property(d => d.ChecklistItemId).HasColumnName("checklist_item_id").IsRequired();
            entity.Property(d => d.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(d => new { d.CaseId, d.ChecklistItemId }).IsUnique();
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Id).HasColumnName("id");
            entity.Property(v => v.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(v => v.VersionNumber).HasColumnName("version_number").IsRequired();
            entity.Property(v => v.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
            entity.Property(v => v.BlobName).HasColumnName("blob_name").HasMaxLength(400).IsRequired();
            entity.Property(v => v.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
            entity.Property(v => v.SizeBytes).HasColumnName("size_bytes").IsRequired();
            entity.Property(v => v.Sha256).HasColumnName("sha256").HasColumnType("char(64)").HasMaxLength(64).IsRequired();
            entity.Property(v => v.UploadedBy).HasColumnName("uploaded_by").IsRequired();
            entity.Property(v => v.UploadedAt).HasColumnName("uploaded_at").IsRequired();

            entity.HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();

            entity.HasOne(v => v.Document)
                  .WithMany(d => d.Versions)
                  .HasForeignKey(v => v.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
