using System.Collections.Concurrent;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Infrastructure.Jobs;

public sealed class InMemoryDocumentJobRepository : IDocumentJobRepository
{
    private readonly ConcurrentDictionary<DocumentJobId, DocumentJob> _jobs = new();
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _idempotencyRecords = new(StringComparer.Ordinal);

    public Task AddAsync(DocumentJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DocumentJob job, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<DocumentJob?> GetByIdAsync(DocumentJobId jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _jobs.TryGetValue(jobId, out DocumentJob? job);
        return Task.FromResult(job);
    }

    public Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _idempotencyRecords.TryGetValue(key, out IdempotencyRecord? record);
        return Task.FromResult(record);
    }

    public Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _idempotencyRecords[record.Key] = record;
        return Task.CompletedTask;
    }
}
