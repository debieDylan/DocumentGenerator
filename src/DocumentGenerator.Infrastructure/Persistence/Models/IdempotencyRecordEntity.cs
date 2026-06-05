namespace DocumentGenerator.Infrastructure.Persistence.Models;

public sealed class IdempotencyRecordEntity
{
    public string Key { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;

    public Guid? JobId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
