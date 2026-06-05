using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Application.Documents.ProcessDocumentGeneration;

public sealed class ProcessDocumentGenerationHandler(
    IDocumentJobRepository jobs,
    ITemplateRepository templates,
    IDocumentRenderer renderer,
    IDocumentStorage storage,
    IClock clock)
{
    public async Task<Result<DocumentJobId>> HandleAsync(DocumentJobId jobId, CancellationToken cancellationToken)
    {
        DocumentJob? job = await jobs.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Result<DocumentJobId>.Failure(new Error("job.not_found", "The document generation job was not found."));
        }

        if (job.Status is DocumentJobStatus.Completed)
        {
            return Result<DocumentJobId>.Success(job.Id);
        }

        DocumentTemplate? template = await templates.GetAsync(job.TemplateId, job.TemplateVersion, cancellationToken);
        if (template is null)
        {
            job.MarkFailed("Template version was not found.", clock.UtcNow);
            await jobs.UpdateAsync(job, cancellationToken);
            return Result<DocumentJobId>.Failure(new Error("template.not_found", "The requested template version was not found."));
        }

        job.MarkProcessing(clock.UtcNow);
        await jobs.UpdateAsync(job, cancellationToken);

        await using Stream content = await renderer.RenderAsync(job, template, cancellationToken);
        DocumentArtifact artifact = await storage.SaveAsync(job, content, cancellationToken);

        job.MarkCompleted(artifact, clock.UtcNow);
        await jobs.UpdateAsync(job, cancellationToken);

        return Result<DocumentJobId>.Success(job.Id);
    }
}
