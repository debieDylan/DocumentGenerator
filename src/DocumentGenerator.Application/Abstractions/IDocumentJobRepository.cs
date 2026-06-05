using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Abstractions;

public interface IDocumentJobRepository
{
    Task AddAsync(DocumentJob job, CancellationToken cancellationToken);

    Task UpdateAsync(DocumentJob job, CancellationToken cancellationToken);

    Task<DocumentJob?> GetByIdAsync(DocumentJobId jobId, CancellationToken cancellationToken);

    Task<Result<SubmitDocumentJobResult>> SubmitAsync(
        DocumentJob job,
        IdempotencyRecord? idempotencyRecord,
        CancellationToken cancellationToken);
}

public sealed record SubmitDocumentJobResult(DocumentJob Job, bool ReusedExistingJob);

public interface IDocumentJobClaimer
{
    Task<Result<DocumentJob>> TryClaimJobAsync(
        DocumentJobId jobId,
        string workerId,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken);
}
