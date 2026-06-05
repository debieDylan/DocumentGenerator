using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.CreateDownloadUrl;
using DocumentGenerator.Application.Documents.GetDocumentStatus;
using DocumentGenerator.Application.Documents.ProcessDocumentGeneration;
using DocumentGenerator.Application.Documents.SubmitDocumentGeneration;
using DocumentGenerator.Infrastructure.Jobs;
using DocumentGenerator.Infrastructure.Queue;
using DocumentGenerator.Infrastructure.Rendering;
using DocumentGenerator.Infrastructure.Storage;
using DocumentGenerator.Infrastructure.Templates;
using DocumentGenerator.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentGenerator.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDocumentJobRepository, InMemoryDocumentJobRepository>();
        services.AddSingleton<IDocumentQueue, InMemoryDocumentQueue>();
        services.AddSingleton<IDocumentStorage, InMemoryDocumentStorage>();
        services.AddSingleton<ITemplateRepository, InMemoryTemplateRepository>();
        services.AddSingleton<IDocumentRenderer, PlaceholderDocumentRenderer>();

        services.AddScoped<SubmitDocumentGenerationHandler>();
        services.AddScoped<GetDocumentStatusHandler>();
        services.AddScoped<CreateDownloadUrlHandler>();
        services.AddScoped<ProcessDocumentGenerationHandler>();

        return services;
    }
}
