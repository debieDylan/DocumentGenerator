using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.ProcessDocumentGeneration;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Worker;

internal sealed class Worker(
    IDocumentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DocumentJobId? jobId = await queue.DequeueAsync(stoppingToken);
                if (jobId is null)
                {
                    continue;
                }

                using IServiceScope scope = scopeFactory.CreateScope();
                ProcessDocumentGenerationHandler handler =
                    scope.ServiceProvider.GetRequiredService<ProcessDocumentGenerationHandler>();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Processing document job {JobId}", jobId.Value);
                }

                await handler.HandleAsync(jobId.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Document worker failed while processing a job.");
            }
        }
    }
}
