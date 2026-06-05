namespace DocumentGenerator.Infrastructure.Persistence.Models;

public sealed class DocumentJobEntity
{
    public Guid Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TemplateId { get; set; } = string.Empty;

    public string TemplateVersion { get; set; } = string.Empty;

    public string OutputFormat { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;

    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public string? FailureReason { get; set; }

    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimExpiresAt { get; set; }

    public int RetryCount { get; set; }

    public Guid? ArtifactId { get; set; }

    public DocumentArtifactEntity? Artifact { get; set; }
}
