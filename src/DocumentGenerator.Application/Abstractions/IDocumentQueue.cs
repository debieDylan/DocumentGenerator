using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Abstractions;

public interface IDocumentQueue
{
    Task EnqueueAsync(DocumentJobId jobId, CancellationToken cancellationToken);

    ValueTask<DocumentJobId?> DequeueAsync(CancellationToken cancellationToken);
}
