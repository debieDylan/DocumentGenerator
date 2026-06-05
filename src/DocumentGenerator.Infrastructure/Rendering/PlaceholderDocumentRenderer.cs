using System.Text;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Infrastructure.Rendering;

public sealed class PlaceholderDocumentRenderer : IDocumentRenderer
{
    public Task<Stream> RenderAsync(DocumentJob job, DocumentTemplate template, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string content = $"Placeholder artifact for job {job.Id} using template {template.TemplateId}:{template.Version}.";
        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return Task.FromResult(stream);
    }
}
