using System.Collections.Concurrent;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Infrastructure.Jobs;

public sealed class InMemoryDocumentJobRepository : IDocumentJobRepository, IDocumentJobClaimer
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

    public Task<Result<SubmitDocumentJobResult>> SubmitAsync(
        DocumentJob job,
        IdempotencyRecord? idempotencyRecord,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (idempotencyRecord is null)
        {
            _jobs[job.Id] = job;
            return Task.FromResult(Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(job, false)));
        }

        if (_idempotencyRecords.TryGetValue(idempotencyRecord.Key, out IdempotencyRecord? existingRecord))
        {
            if (!string.Equals(existingRecord.PayloadHash, idempotencyRecord.PayloadHash, StringComparison.Ordinal))
            {
                return Task.FromResult(Result<SubmitDocumentJobResult>.Failure(
                    new Error("idempotency.conflict", "The idempotency key was already used with a different payload.")));
            }

            if (_jobs.TryGetValue(existingRecord.JobId, out DocumentJob? existingJob))
            {
                return Task.FromResult(Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(existingJob, true)));
            }
        }

        _jobs[job.Id] = job;
        _idempotencyRecords[idempotencyRecord.Key] = idempotencyRecord;
        return Task.FromResult(Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(job, false)));
    }

    public Task<Result<DocumentJob>> TryClaimJobAsync(
        DocumentJobId jobId,
        string workerId,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_jobs.TryGetValue(jobId, out DocumentJob? job))
        {
            return Task.FromResult(Result<DocumentJob>.Failure(
                new Error("job.not_found", "The document generation job was not found.")));
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool claimable = job.Status is DocumentJobStatus.Pending
            || job.Status is DocumentJobStatus.Processing && job.ClaimExpiresAt <= now;

        if (!claimable)
        {
            return Task.FromResult(Result<DocumentJob>.Failure(
                new Error("job.claim_failed", "The document generation job could not be claimed.")));
        }

        job.MarkClaimed(workerId, claimExpiresAt, now);
        return Task.FromResult(Result<DocumentJob>.Success(job));
    }
}
