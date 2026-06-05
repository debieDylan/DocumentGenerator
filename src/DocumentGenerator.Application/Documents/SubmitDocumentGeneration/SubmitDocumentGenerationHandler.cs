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

        DocumentJob job = new(
            DocumentJobId.New(),
            command.TemplateId,
            command.TemplateVersion,
            command.OutputFormat,
            payloadHash,
            command.IdempotencyKey,
            clock.UtcNow);

        IdempotencyRecord? record = null;
        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            record = new IdempotencyRecord(command.IdempotencyKey, payloadHash, job.Id, clock.UtcNow);
        }

        Result<SubmitDocumentJobResult> submission = await jobs.SubmitAsync(job, record, cancellationToken);
        if (submission.IsFailure)
        {
            return Result<SubmitDocumentGenerationResponse>.Failure(submission.Error!);
        }

        SubmitDocumentJobResult submitted = submission.Value!;
        if (!submitted.ReusedExistingJob)
        {
            await queue.EnqueueAsync(submitted.Job.Id, cancellationToken);
        }

        return Result<SubmitDocumentGenerationResponse>.Success(
            new SubmitDocumentGenerationResponse(
                submitted.Job.Id,
                submitted.Job.Status,
                submitted.ReusedExistingJob));
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
