namespace DocumentGenerator.Application.Documents.CreateDownloadUrl;

public sealed record CreateDownloadUrlResponse(Uri Url, DateTimeOffset ExpiresAt);
