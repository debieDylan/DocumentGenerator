using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.ProcessDocumentGeneration;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Worker;

internal sealed class Worker(
    IDocumentQueue queue,
    IClock clock,
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly string _workerId = string.Concat(Environment.MachineName, "-", Guid.NewGuid().ToString("N"));

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

                ProcessDocumentGenerationCommand command = new(
                    jobId.Value,
                    _workerId,
                    clock.UtcNow.AddMinutes(5));

                await handler.HandleAsync(command, stoppingToken);
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
