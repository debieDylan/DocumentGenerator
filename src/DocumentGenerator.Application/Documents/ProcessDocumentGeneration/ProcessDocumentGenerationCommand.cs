using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.ProcessDocumentGeneration;

public sealed record ProcessDocumentGenerationCommand(
    DocumentJobId JobId,
    string WorkerId,
    DateTimeOffset ClaimExpiresAt);
