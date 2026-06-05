using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Domain.Artifacts;

public readonly record struct DocumentArtifactId(Guid Value)
{
    public static DocumentArtifactId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

public sealed record DocumentArtifact(
    DocumentArtifactId Id,
    DocumentJobId JobId,
    Uri StorageUri,
    OutputFormat Format,
    long SizeInBytes,
    string ContentType,
    DateTimeOffset CreatedAt);
