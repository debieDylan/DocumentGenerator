using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.CreateDownloadUrl;

public sealed class CreateDownloadUrlHandler(
    IDocumentJobRepository jobs,
    IDocumentStorage storage,
    IClock clock)
{
    public async Task<Result<CreateDownloadUrlResponse>> HandleAsync(
        CreateDownloadUrlQuery query,
        CancellationToken cancellationToken)
    {
        DocumentJob? job = await jobs.GetByIdAsync(query.JobId, cancellationToken);

        if (job is null)
        {
            return Result<CreateDownloadUrlResponse>.Failure(
                new Error("job.not_found", "The document generation job was not found."));
        }

        if (job.Status is not DocumentJobStatus.Completed || job.Artifact is null)
        {
            return Result<CreateDownloadUrlResponse>.Failure(
                new Error("artifact.not_ready", "The document artifact is not ready for download."));
        }

        Uri url = await storage.CreateDownloadUrlAsync(job.Artifact, query.ExpiresIn, cancellationToken);
        return Result<CreateDownloadUrlResponse>.Success(
            new CreateDownloadUrlResponse(url, clock.UtcNow.Add(query.ExpiresIn)));
    }
}
