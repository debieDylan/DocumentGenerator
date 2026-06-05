using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Domain.Idempotency;

public sealed record IdempotencyRecord(
    string Key,
    string PayloadHash,
    DocumentJobId JobId,
    DateTimeOffset CreatedAt);
