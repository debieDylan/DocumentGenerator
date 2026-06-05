using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Abstractions;

public interface IDocumentStorage
{
    Task<DocumentArtifact> SaveAsync(DocumentJob job, Stream content, CancellationToken cancellationToken);

    Task<Uri> CreateDownloadUrlAsync(DocumentArtifact artifact, TimeSpan expiresIn, CancellationToken cancellationToken);
}
