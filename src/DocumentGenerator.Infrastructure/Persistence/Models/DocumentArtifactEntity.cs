namespace DocumentGenerator.Infrastructure.Persistence.Models;

public sealed class DocumentArtifactEntity
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public string StorageLocation { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
