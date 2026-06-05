using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Application.Documents.SubmitDocumentGeneration;

public sealed class SubmitDocumentGenerationHandler(
    IDocumentJobRepository jobs,
    ITemplateRepository templates,
    IDocumentQueue queue,
    IClock clock)
{
    public async Task<Result<SubmitDocumentGenerationResponse>> HandleAsync(
        SubmitDocumentGenerationCommand command,
        CancellationToken cancellationToken)
    {
        DocumentTemplate? template = await templates.GetAsync(
            command.TemplateId,
            command.TemplateVersion,
            cancellationToken);

        if (template is null)
        {
            return Result<SubmitDocumentGenerationResponse>.Failure(
                new Error("template.not_found", "The requested template version was not found."));
        }

        string payloadHash = ComputePayloadHash(command);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            IdempotencyRecord? existingRecord = await jobs.GetIdempotencyRecordAsync(
                command.IdempotencyKey,
                cancellationToken);

            if (existingRecord is not null)
            {
                if (!string.Equals(existingRecord.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    return Result<SubmitDocumentGenerationResponse>.Failure(
                        new Error("idempotency.conflict", "The idempotency key was already used with a different payload."));
                }

                DocumentJob? existingJob = await jobs.GetByIdAsync(existingRecord.JobId, cancellationToken);
                if (existingJob is not null)
                {
                    return Result<SubmitDocumentGenerationResponse>.Success(
                        new SubmitDocumentGenerationResponse(existingJob.Id, existingJob.Status, true));
                }
            }
        }

        DocumentJob job = new(
            DocumentJobId.New(),
            command.TemplateId,
            command.TemplateVersion,
            command.OutputFormat,
            payloadHash,
            command.IdempotencyKey,
            clock.UtcNow);

        await jobs.AddAsync(job, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            IdempotencyRecord record = new(command.IdempotencyKey, payloadHash, job.Id, clock.UtcNow);
            await jobs.AddIdempotencyRecordAsync(record, cancellationToken);
        }

        await queue.EnqueueAsync(job.Id, cancellationToken);

        return Result<SubmitDocumentGenerationResponse>.Success(
            new SubmitDocumentGenerationResponse(job.Id, job.Status, false));
    }

    private static string ComputePayloadHash(SubmitDocumentGenerationCommand command)
    {
        string payload = JsonSerializer.Serialize(new
        {
            command.TemplateId,
            command.TemplateVersion,
            command.OutputFormat,
            command.InputData,
        });

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
