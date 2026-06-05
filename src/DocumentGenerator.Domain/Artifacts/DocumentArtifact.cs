using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Domain.Artifacts;

public sealed record DocumentArtifact(
    DocumentJobId JobId,
    Uri StorageUri,
    OutputFormat Format,
    long SizeInBytes,
    string ContentType,
    DateTimeOffset CreatedAt);
