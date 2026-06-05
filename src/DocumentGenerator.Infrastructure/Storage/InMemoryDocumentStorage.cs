using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Infrastructure.Storage;

public sealed class InMemoryDocumentStorage : IDocumentStorage
{
    public async Task<DocumentArtifact> SaveAsync(DocumentJob job, Stream content, CancellationToken cancellationToken)
    {
        long sizeInBytes = 0;
        byte[] buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            sizeInBytes += bytesRead;
        }

        return new DocumentArtifact(
            DocumentArtifactId.New(),
            job.Id,
            new Uri($"memory://documents/{job.Id}/artifact{GetExtension(job.OutputFormat)}"),
            job.OutputFormat,
            sizeInBytes,
            GetContentType(job.OutputFormat),
            DateTimeOffset.UtcNow);
    }

    public Task<Uri> CreateDownloadUrlAsync(DocumentArtifact artifact, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Uri uri = new($"{artifact.StorageUri}?expiresInSeconds={(int)expiresIn.TotalSeconds}");
        return Task.FromResult(uri);
    }

    private static string GetContentType(OutputFormat format) =>
        format switch
        {
            OutputFormat.Pdf => "application/pdf",
            OutputFormat.Csv => "text/csv",
            OutputFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            OutputFormat.Zip => "application/zip",
            _ => "application/octet-stream",
        };

    private static string GetExtension(OutputFormat format) =>
        format switch
        {
            OutputFormat.Pdf => ".pdf",
            OutputFormat.Csv => ".csv",
            OutputFormat.Xlsx => ".xlsx",
            OutputFormat.Zip => ".zip",
            _ => ".bin",
        };
}
