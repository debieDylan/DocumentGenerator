using DocumentGenerator.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerator.Infrastructure.Persistence;

public sealed class DocumentGeneratorDbContext(DbContextOptions<DocumentGeneratorDbContext> options) : DbContext(options)
{
    public DbSet<DocumentJobEntity> Jobs => Set<DocumentJobEntity>();

    public DbSet<DocumentArtifactEntity> Artifacts => Set<DocumentArtifactEntity>();

    public DbSet<DocumentTemplateEntity> Templates => Set<DocumentTemplateEntity>();

    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentJobEntity>(entity =>
        {
            entity.ToTable("document_jobs");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Id).HasColumnName("id");
            entity.Property(job => job.Status).HasColumnName("status").HasMaxLength(32);
            entity.Property(job => job.TemplateId).HasColumnName("template_id").HasMaxLength(200);
            entity.Property(job => job.TemplateVersion).HasColumnName("template_version").HasMaxLength(100);
            entity.Property(job => job.OutputFormat).HasColumnName("output_format").HasMaxLength(32);
            entity.Property(job => job.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128);
            entity.Property(job => job.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
            entity.Property(job => job.CreatedAt).HasColumnName("created_at");
            entity.Property(job => job.UpdatedAt).HasColumnName("updated_at");
            entity.Property(job => job.StartedAt).HasColumnName("started_at");
            entity.Property(job => job.CompletedAt).HasColumnName("completed_at");
            entity.Property(job => job.FailedAt).HasColumnName("failed_at");
            entity.Property(job => job.FailureReason).HasColumnName("failure_reason");
            entity.Property(job => job.ClaimedBy).HasColumnName("claimed_by").HasMaxLength(200);
            entity.Property(job => job.ClaimExpiresAt).HasColumnName("claim_expires_at");
            entity.Property(job => job.RetryCount).HasColumnName("retry_count");
            entity.Property(job => job.ArtifactId).HasColumnName("artifact_id");
            entity.HasOne(job => job.Artifact).WithOne().HasForeignKey<DocumentJobEntity>(job => job.ArtifactId);
        });

        modelBuilder.Entity<DocumentArtifactEntity>(entity =>
        {
            entity.ToTable("document_artifacts");
            entity.HasKey(artifact => artifact.Id);
            entity.Property(artifact => artifact.Id).HasColumnName("id");
            entity.Property(artifact => artifact.JobId).HasColumnName("job_id");
            entity.Property(artifact => artifact.StorageLocation).HasColumnName("storage_uri");
            entity.Property(artifact => artifact.Format).HasColumnName("format").HasMaxLength(32);
            entity.Property(artifact => artifact.SizeInBytes).HasColumnName("size_in_bytes");
            entity.Property(artifact => artifact.ContentType).HasColumnName("content_type").HasMaxLength(200);
            entity.Property(artifact => artifact.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(artifact => artifact.JobId).IsUnique();
        });

        modelBuilder.Entity<DocumentTemplateEntity>(entity =>
        {
            entity.ToTable("document_templates");
            entity.HasKey(template => new { template.TemplateId, template.Version });
            entity.Property(template => template.TemplateId).HasColumnName("template_id").HasMaxLength(200);
            entity.Property(template => template.Version).HasColumnName("version").HasMaxLength(100);
            entity.Property(template => template.Renderer).HasColumnName("renderer").HasMaxLength(200);
            entity.Property(template => template.RegisteredAt).HasColumnName("registered_at");
        });

        modelBuilder.Entity<IdempotencyRecordEntity>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(record => record.Key);
            entity.Property(record => record.Key).HasColumnName("key").HasMaxLength(200);
            entity.Property(record => record.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128);
            entity.Property(record => record.JobId).HasColumnName("job_id");
            entity.Property(record => record.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(record => record.JobId).IsUnique();
        });
    }
}
