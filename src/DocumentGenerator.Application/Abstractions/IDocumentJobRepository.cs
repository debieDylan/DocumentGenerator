using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Abstractions;

public interface IDocumentJobRepository
{
    Task AddAsync(DocumentJob job, CancellationToken cancellationToken);

    Task UpdateAsync(DocumentJob job, CancellationToken cancellationToken);

    Task<DocumentJob?> GetByIdAsync(DocumentJobId jobId, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken cancellationToken);

    Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}
