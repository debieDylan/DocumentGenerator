using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.GetDocumentStatus;

public sealed record GetDocumentStatusResponse(
    DocumentJobId JobId,
    DocumentJobStatus Status,
    string TemplateId,
    string TemplateVersion,
    OutputFormat OutputFormat,
    Uri? ArtifactUri,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
