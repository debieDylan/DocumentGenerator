namespace DocumentGenerator.Contracts.Documents;

public sealed record DocumentStatusResponse(
    Guid JobId,
    string Status,
    string TemplateId,
    string TemplateVersion,
    string OutputFormat,
    Uri? ArtifactUri,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
