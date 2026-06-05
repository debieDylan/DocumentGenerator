using DocumentGenerator.Domain.Artifacts;

namespace DocumentGenerator.Domain.Jobs;

public sealed class DocumentJob
{
    public DocumentJob(
        DocumentJobId id,
        string templateId,
        string templateVersion,
        OutputFormat outputFormat,
        string inputDataHash,
        string? idempotencyKey,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null,
        DocumentJobStatus status = DocumentJobStatus.Pending,
        DocumentArtifact? artifact = null,
        string? failureReason = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? failedAt = null,
        string? claimedBy = null,
        DateTimeOffset? claimExpiresAt = null,
        int retryCount = 0)
    {
        Id = id;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        OutputFormat = outputFormat;
        InputDataHash = inputDataHash;
        IdempotencyKey = idempotencyKey;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt ?? createdAt;
        Status = status;
        Artifact = artifact;
        FailureReason = failureReason;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        FailedAt = failedAt;
        ClaimedBy = claimedBy;
        ClaimExpiresAt = claimExpiresAt;
        RetryCount = retryCount;
    }

    public DocumentJobId Id { get; }

    public string TemplateId { get; }

    public string TemplateVersion { get; }

    public OutputFormat OutputFormat { get; }

    public string InputDataHash { get; }

    public string? IdempotencyKey { get; }

    public DocumentJobStatus Status { get; private set; }

    public DocumentArtifact? Artifact { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? ClaimedBy { get; private set; }

    public DateTimeOffset? ClaimExpiresAt { get; private set; }

    public int RetryCount { get; private set; }

    public void MarkClaimed(string workerId, DateTimeOffset claimExpiresAt, DateTimeOffset claimedAt)
    {
        Status = DocumentJobStatus.Processing;
        ClaimedBy = workerId;
        ClaimExpiresAt = claimExpiresAt;
        RetryCount++;
        StartedAt ??= claimedAt;
        UpdatedAt = claimedAt;
    }

    public void MarkProcessing(DateTimeOffset startedAt)
    {
        if (Status is DocumentJobStatus.Completed)
        {
            return;
        }

        Status = DocumentJobStatus.Processing;
        StartedAt ??= startedAt;
        UpdatedAt = startedAt;
    }

    public void MarkCompleted(DocumentArtifact artifact, DateTimeOffset completedAt)
    {
        Artifact = artifact;
        CompletedAt = completedAt;
        FailureReason = null;
        ClaimedBy = null;
        ClaimExpiresAt = null;
        Status = DocumentJobStatus.Completed;
        UpdatedAt = completedAt;
    }

    public void MarkFailed(string failureReason, DateTimeOffset failedAt)
    {
        FailureReason = failureReason;
        FailedAt = failedAt;
        ClaimedBy = null;
        ClaimExpiresAt = null;
        Status = DocumentJobStatus.Failed;
        UpdatedAt = failedAt;
    }
}
