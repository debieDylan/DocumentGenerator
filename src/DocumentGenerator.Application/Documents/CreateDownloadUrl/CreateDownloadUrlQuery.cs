using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.CreateDownloadUrl;

public sealed record CreateDownloadUrlQuery(DocumentJobId JobId, TimeSpan ExpiresIn);
