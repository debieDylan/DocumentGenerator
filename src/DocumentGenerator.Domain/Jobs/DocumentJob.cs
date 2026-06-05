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
        DateTimeOffset createdAt)
    {
        Id = id;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        OutputFormat = outputFormat;
        InputDataHash = inputDataHash;
        IdempotencyKey = idempotencyKey;
        CreatedAt = createdAt;
        Status = DocumentJobStatus.Pending;
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

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkProcessing(DateTimeOffset startedAt)
    {
        if (Status is DocumentJobStatus.Completed)
        {
            return;
        }

        Status = DocumentJobStatus.Processing;
        StartedAt ??= startedAt;
    }

    public void MarkCompleted(DocumentArtifact artifact, DateTimeOffset completedAt)
    {
        Artifact = artifact;
        CompletedAt = completedAt;
        FailureReason = null;
        Status = DocumentJobStatus.Completed;
    }

    public void MarkFailed(string failureReason, DateTimeOffset failedAt)
    {
        FailureReason = failureReason;
        CompletedAt = failedAt;
        Status = DocumentJobStatus.Failed;
    }
}
