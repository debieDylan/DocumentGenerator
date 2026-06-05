using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.GetDocumentStatus;

public sealed record GetDocumentStatusQuery(DocumentJobId JobId);
