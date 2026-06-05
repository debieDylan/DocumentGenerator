using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Application.Abstractions;

public interface IDocumentRenderer
{
    Task<Stream> RenderAsync(DocumentJob job, DocumentTemplate template, CancellationToken cancellationToken);
}
