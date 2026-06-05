namespace DocumentGenerator.Contracts.Documents;

public sealed record DownloadUrlResponse(Uri Url, DateTimeOffset ExpiresAt);
