using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.SubmitDocumentGeneration;

public sealed record SubmitDocumentGenerationResponse(
    DocumentJobId JobId,
    DocumentJobStatus Status,
    bool ReusedExistingJob);
