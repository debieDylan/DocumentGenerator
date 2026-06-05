using System.Data;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;
using Npgsql;
using NpgsqlTypes;

namespace DocumentGenerator.Infrastructure.Persistence.Repositories;

public sealed class PostgresDocumentJobRepository(NpgsqlDataSource dataSource, IClock clock) :
    IDocumentJobRepository,
    IDocumentJobClaimer
{
    public async Task AddAsync(DocumentJob job, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await InsertJobAsync(connection, null, job, cancellationToken);
    }

    public async Task UpdateAsync(DocumentJob job, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid? artifactId = null;
        if (job.Artifact is not null)
        {
            artifactId = job.Artifact.Id.Value;
            await UpsertArtifactAsync(connection, transaction, job.Artifact, cancellationToken);
        }

        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE document_jobs
            SET status = @status,
                updated_at = @updated_at,
                started_at = @started_at,
                completed_at = @completed_at,
                failed_at = @failed_at,
                failure_reason = @failure_reason,
                claimed_by = @claimed_by,
                claim_expires_at = @claim_expires_at,
                retry_count = @retry_count,
                artifact_id = @artifact_id
            WHERE id = @id;
            """;
        AddJobUpdateParameters(command, job, artifactId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<DocumentJob?> GetByIdAsync(DocumentJobId jobId, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await GetByIdAsync(connection, null, jobId, cancellationToken);
    }

    public async Task<Result<SubmitDocumentJobResult>> SubmitAsync(
        DocumentJob job,
        IdempotencyRecord? idempotencyRecord,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        if (idempotencyRecord is null)
        {
            await InsertJobAsync(connection, transaction, job, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(job, false));
        }

        bool inserted = await TryInsertIdempotencyRecordAsync(connection, transaction, idempotencyRecord, cancellationToken);
        if (inserted)
        {
            await InsertJobAsync(connection, transaction, job, cancellationToken);
            await AttachJobToIdempotencyRecordAsync(connection, transaction, idempotencyRecord.Key, job.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(job, false));
        }

        IdempotencyRecord? existingRecord = await GetIdempotencyRecordAsync(
            connection,
            transaction,
            idempotencyRecord.Key,
            cancellationToken);

        if (existingRecord is null || existingRecord.JobId.Value == Guid.Empty)
        {
            return Result<SubmitDocumentJobResult>.Failure(
                new Error("idempotency.incomplete", "The idempotency record is not linked to a document job."));
        }

        if (!string.Equals(existingRecord.PayloadHash, idempotencyRecord.PayloadHash, StringComparison.Ordinal))
        {
            return Result<SubmitDocumentJobResult>.Failure(
                new Error("idempotency.conflict", "The idempotency key was already used with a different payload."));
        }

        DocumentJob? existingJob = await GetByIdAsync(connection, transaction, existingRecord.JobId, cancellationToken);
        if (existingJob is null)
        {
            return Result<SubmitDocumentJobResult>.Failure(
                new Error("job.not_found", "The idempotency record points to a missing document job."));
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<SubmitDocumentJobResult>.Success(new SubmitDocumentJobResult(existingJob, true));
    }

    public async Task<Result<DocumentJob>> TryClaimJobAsync(
        DocumentJobId jobId,
        string workerId,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE document_jobs
            SET status = 'Processing',
                claimed_by = @worker_id,
                claim_expires_at = @claim_expires_at,
                started_at = COALESCE(started_at, @now),
                updated_at = @now,
                retry_count = retry_count + 1
            WHERE id = @id
              AND (
                    status = 'Pending'
                    OR (status = 'Processing' AND claim_expires_at IS NOT NULL AND claim_expires_at <= @now)
                  )
            RETURNING id;
            """;
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, jobId.Value);
        command.Parameters.AddWithValue("worker_id", NpgsqlDbType.Text, workerId);
        command.Parameters.AddWithValue("claim_expires_at", NpgsqlDbType.TimestampTz, claimExpiresAt);
        command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);

        object? claimedId = await command.ExecuteScalarAsync(cancellationToken);
        if (claimedId is null)
        {
            DocumentJob? existingJob = await GetByIdAsync(connection, null, jobId, cancellationToken);
            Error error = existingJob is null
                ? new Error("job.not_found", "The document generation job was not found.")
                : new Error("job.claim_failed", "The document generation job could not be claimed.");

            return Result<DocumentJob>.Failure(error);
        }

        DocumentJob? claimedJob = await GetByIdAsync(connection, null, jobId, cancellationToken);
        return claimedJob is null
            ? Result<DocumentJob>.Failure(new Error("job.not_found", "The document generation job was not found."))
            : Result<DocumentJob>.Success(claimedJob);
    }

    private static async Task InsertJobAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        DocumentJob job,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO document_jobs(
                id,
                status,
                template_id,
                template_version,
                output_format,
                payload_hash,
                idempotency_key,
                created_at,
                updated_at,
                retry_count)
            VALUES (
                @id,
                @status,
                @template_id,
                @template_version,
                @output_format,
                @payload_hash,
                @idempotency_key,
                @created_at,
                @updated_at,
                @retry_count);
            """;
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, job.Id.Value);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Text, job.Status.ToString());
        command.Parameters.AddWithValue("template_id", NpgsqlDbType.Text, job.TemplateId);
        command.Parameters.AddWithValue("template_version", NpgsqlDbType.Text, job.TemplateVersion);
        command.Parameters.AddWithValue("output_format", NpgsqlDbType.Text, job.OutputFormat.ToString());
        command.Parameters.AddWithValue("payload_hash", NpgsqlDbType.Text, job.InputDataHash);
        command.Parameters.AddWithValue("idempotency_key", NpgsqlDbType.Text, (object?)job.IdempotencyKey ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, job.CreatedAt);
        command.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, job.UpdatedAt);
        command.Parameters.AddWithValue("retry_count", NpgsqlDbType.Integer, job.RetryCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TryInsertIdempotencyRecordAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IdempotencyRecord record,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO idempotency_records(key, payload_hash, created_at)
            VALUES (@key, @payload_hash, @created_at)
            ON CONFLICT (key) DO NOTHING;
            """;
        command.Parameters.AddWithValue("key", NpgsqlDbType.Text, record.Key);
        command.Parameters.AddWithValue("payload_hash", NpgsqlDbType.Text, record.PayloadHash);
        command.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, record.CreatedAt);
        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows == 1;
    }

    private static async Task AttachJobToIdempotencyRecordAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string key,
        DocumentJobId jobId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE idempotency_records SET job_id = @job_id WHERE key = @key;";
        command.Parameters.AddWithValue("job_id", NpgsqlDbType.Uuid, jobId.Value);
        command.Parameters.AddWithValue("key", NpgsqlDbType.Text, key);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IdempotencyRecord?> GetIdempotencyRecordAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string key,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT key, payload_hash, job_id, created_at
            FROM idempotency_records
            WHERE key = @key;
            """;
        command.Parameters.AddWithValue("key", NpgsqlDbType.Text, key);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        Guid jobId = await reader.IsDBNullAsync(2, cancellationToken) ? Guid.Empty : reader.GetGuid(2);
        return new IdempotencyRecord(
            reader.GetString(0),
            reader.GetString(1),
            new DocumentJobId(jobId),
            await reader.GetFieldValueAsync<DateTimeOffset>(3, cancellationToken));
    }

    private static async Task<DocumentJob?> GetByIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        DocumentJobId jobId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                j.id,
                j.status,
                j.template_id,
                j.template_version,
                j.output_format,
                j.payload_hash,
                j.idempotency_key,
                j.created_at,
                j.updated_at,
                j.started_at,
                j.completed_at,
                j.failed_at,
                j.failure_reason,
                j.claimed_by,
                j.claim_expires_at,
                j.retry_count,
                a.id,
                a.storage_uri,
                a.format,
                a.size_in_bytes,
                a.content_type,
                a.created_at
            FROM document_jobs j
            LEFT JOIN document_artifacts a ON a.id = j.artifact_id
            WHERE j.id = @id;
            """;
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, jobId.Value);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return await MapJobAsync(reader, cancellationToken);
    }

    private static async Task<DocumentJob> MapJobAsync(NpgsqlDataReader reader, CancellationToken cancellationToken)
    {
        DocumentArtifact? artifact = null;
        if (!await reader.IsDBNullAsync(16, cancellationToken))
        {
            artifact = new DocumentArtifact(
                new DocumentArtifactId(reader.GetGuid(16)),
                new DocumentJobId(reader.GetGuid(0)),
                new Uri(reader.GetString(17)),
                Enum.Parse<OutputFormat>(reader.GetString(18)),
                reader.GetInt64(19),
                reader.GetString(20),
                await reader.GetFieldValueAsync<DateTimeOffset>(21, cancellationToken));
        }

        return new DocumentJob(
            new DocumentJobId(reader.GetGuid(0)),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<OutputFormat>(reader.GetString(4)),
            reader.GetString(5),
            await reader.IsDBNullAsync(6, cancellationToken) ? null : reader.GetString(6),
            await reader.GetFieldValueAsync<DateTimeOffset>(7, cancellationToken),
            await reader.GetFieldValueAsync<DateTimeOffset>(8, cancellationToken),
            Enum.Parse<DocumentJobStatus>(reader.GetString(1)),
            artifact,
            await reader.IsDBNullAsync(12, cancellationToken) ? null : reader.GetString(12),
            await reader.IsDBNullAsync(9, cancellationToken) ? null : await reader.GetFieldValueAsync<DateTimeOffset>(9, cancellationToken),
            await reader.IsDBNullAsync(10, cancellationToken) ? null : await reader.GetFieldValueAsync<DateTimeOffset>(10, cancellationToken),
            await reader.IsDBNullAsync(11, cancellationToken) ? null : await reader.GetFieldValueAsync<DateTimeOffset>(11, cancellationToken),
            await reader.IsDBNullAsync(13, cancellationToken) ? null : reader.GetString(13),
            await reader.IsDBNullAsync(14, cancellationToken) ? null : await reader.GetFieldValueAsync<DateTimeOffset>(14, cancellationToken),
            reader.GetInt32(15));
    }

    private static async Task UpsertArtifactAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DocumentArtifact artifact,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO document_artifacts(id, job_id, storage_uri, format, size_in_bytes, content_type, created_at)
            VALUES (@id, @job_id, @storage_uri, @format, @size_in_bytes, @content_type, @created_at)
            ON CONFLICT (job_id) DO UPDATE
            SET storage_uri = EXCLUDED.storage_uri,
                format = EXCLUDED.format,
                size_in_bytes = EXCLUDED.size_in_bytes,
                content_type = EXCLUDED.content_type,
                created_at = EXCLUDED.created_at;
            """;
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, artifact.Id.Value);
        command.Parameters.AddWithValue("job_id", NpgsqlDbType.Uuid, artifact.JobId.Value);
        command.Parameters.AddWithValue("storage_uri", NpgsqlDbType.Text, artifact.StorageUri.ToString());
        command.Parameters.AddWithValue("format", NpgsqlDbType.Text, artifact.Format.ToString());
        command.Parameters.AddWithValue("size_in_bytes", NpgsqlDbType.Bigint, artifact.SizeInBytes);
        command.Parameters.AddWithValue("content_type", NpgsqlDbType.Text, artifact.ContentType);
        command.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, artifact.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddJobUpdateParameters(NpgsqlCommand command, DocumentJob job, Guid? artifactId)
    {
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, job.Id.Value);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Text, job.Status.ToString());
        command.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, job.UpdatedAt);
        command.Parameters.AddWithValue("started_at", NpgsqlDbType.TimestampTz, ToDbValue(job.StartedAt));
        command.Parameters.AddWithValue("completed_at", NpgsqlDbType.TimestampTz, ToDbValue(job.CompletedAt));
        command.Parameters.AddWithValue("failed_at", NpgsqlDbType.TimestampTz, ToDbValue(job.FailedAt));
        command.Parameters.AddWithValue("failure_reason", NpgsqlDbType.Text, (object?)job.FailureReason ?? DBNull.Value);
        command.Parameters.AddWithValue("claimed_by", NpgsqlDbType.Text, (object?)job.ClaimedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("claim_expires_at", NpgsqlDbType.TimestampTz, ToDbValue(job.ClaimExpiresAt));
        command.Parameters.AddWithValue("retry_count", NpgsqlDbType.Integer, job.RetryCount);
        command.Parameters.AddWithValue("artifact_id", NpgsqlDbType.Uuid, ToDbValue(artifactId));
    }

    private static object ToDbValue(DateTimeOffset? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object ToDbValue(Guid? value) => value.HasValue ? value.Value : DBNull.Value;
}
