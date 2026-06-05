using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.GetDocumentStatus;

public sealed class GetDocumentStatusHandler(IDocumentJobRepository jobs)
{
    public async Task<Result<GetDocumentStatusResponse>> HandleAsync(
        GetDocumentStatusQuery query,
        CancellationToken cancellationToken)
    {
        DocumentJob? job = await jobs.GetByIdAsync(query.JobId, cancellationToken);

        if (job is null)
        {
            return Result<GetDocumentStatusResponse>.Failure(
                new Error("job.not_found", "The document generation job was not found."));
        }

        return Result<GetDocumentStatusResponse>.Success(
            new GetDocumentStatusResponse(
                job.Id,
                job.Status,
                job.TemplateId,
                job.TemplateVersion,
                job.OutputFormat,
                job.Artifact?.StorageUri,
                job.FailureReason,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt));
    }
}
