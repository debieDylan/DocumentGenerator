using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;
using Npgsql;
using NpgsqlTypes;

namespace DocumentGenerator.Infrastructure.Persistence.Queue;

public sealed class PostgresDocumentQueue(NpgsqlDataSource dataSource, IClock clock) : IDocumentQueue
{
    public Task EnqueueAsync(DocumentJobId jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async ValueTask<DocumentJobId?> DequeueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            DocumentJobId? jobId = await TryGetCandidateAsync(cancellationToken);
            if (jobId is not null)
            {
                return jobId;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return null;
    }

    private async Task<DocumentJobId?> TryGetCandidateAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id
            FROM document_jobs
            WHERE status = 'Pending'
               OR (status = 'Processing' AND claim_expires_at IS NOT NULL AND claim_expires_at <= @now)
            ORDER BY created_at
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, clock.UtcNow);

        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is Guid id ? new DocumentJobId(id) : null;
    }
}
