using System.Threading.Channels;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Infrastructure.Queue;

public sealed class InMemoryDocumentQueue : IDocumentQueue
{
    private readonly Channel<DocumentJobId> _channel = Channel.CreateUnbounded<DocumentJobId>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public async Task EnqueueAsync(DocumentJobId jobId, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(jobId, cancellationToken);

    public async ValueTask<DocumentJobId?> DequeueAsync(CancellationToken cancellationToken)
    {
        DocumentJobId jobId = await _channel.Reader.ReadAsync(cancellationToken);
        return jobId;
    }
}
