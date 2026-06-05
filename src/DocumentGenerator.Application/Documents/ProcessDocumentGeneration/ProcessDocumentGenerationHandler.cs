using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Application.Documents.ProcessDocumentGeneration;

public sealed class ProcessDocumentGenerationHandler(
    IDocumentJobRepository jobs,
    IDocumentJobClaimer claimer,
    ITemplateRepository templates,
    IDocumentRenderer renderer,
    IDocumentStorage storage,
    IClock clock)
{
    public async Task<Result<DocumentJobId>> HandleAsync(
        ProcessDocumentGenerationCommand command,
        CancellationToken cancellationToken)
    {
        Result<DocumentJob> claim = await claimer.TryClaimJobAsync(
            command.JobId,
            command.WorkerId,
            command.ClaimExpiresAt,
            cancellationToken);

        if (claim.IsFailure)
        {
            return Result<DocumentJobId>.Failure(claim.Error!);
        }

        DocumentJob job = claim.Value!;

        DocumentTemplate? template = await templates.GetAsync(job.TemplateId, job.TemplateVersion, cancellationToken);
        if (template is null)
        {
            job.MarkFailed("Template version was not found.", clock.UtcNow);
            await jobs.UpdateAsync(job, cancellationToken);
            return Result<DocumentJobId>.Failure(new Error("template.not_found", "The requested template version was not found."));
        }

        await using Stream content = await renderer.RenderAsync(job, template, cancellationToken);
        DocumentArtifact artifact = await storage.SaveAsync(job, content, cancellationToken);

        job.MarkCompleted(artifact, clock.UtcNow);
        await jobs.UpdateAsync(job, cancellationToken);

        return Result<DocumentJobId>.Success(job.Id);
    }
}
