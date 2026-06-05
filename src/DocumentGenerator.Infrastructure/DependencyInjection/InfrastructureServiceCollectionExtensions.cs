using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.CreateDownloadUrl;
using DocumentGenerator.Application.Documents.GetDocumentStatus;
using DocumentGenerator.Application.Documents.ProcessDocumentGeneration;
using DocumentGenerator.Application.Documents.SubmitDocumentGeneration;
using DocumentGenerator.Infrastructure.Jobs;
using DocumentGenerator.Infrastructure.Persistence;
using DocumentGenerator.Infrastructure.Persistence.Queue;
using DocumentGenerator.Infrastructure.Persistence.Repositories;
using DocumentGenerator.Infrastructure.Persistence.Templates;
using DocumentGenerator.Infrastructure.Queue;
using DocumentGenerator.Infrastructure.Rendering;
using DocumentGenerator.Infrastructure.Storage;
using DocumentGenerator.Infrastructure.Templates;
using DocumentGenerator.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DocumentGenerator.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentGenerator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDocumentStorage, InMemoryDocumentStorage>();
        services.AddSingleton<IDocumentRenderer, PlaceholderDocumentRenderer>();

        string provider = configuration["DocumentGenerator:MetadataProvider"] ?? "PostgreSql";
        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<InMemoryDocumentJobRepository>();
            services.AddSingleton<IDocumentJobRepository>(providerServices =>
                providerServices.GetRequiredService<InMemoryDocumentJobRepository>());
            services.AddSingleton<IDocumentJobClaimer>(providerServices =>
                providerServices.GetRequiredService<InMemoryDocumentJobRepository>());
            services.AddSingleton<IDocumentQueue, InMemoryDocumentQueue>();
            services.AddSingleton<ITemplateRepository, InMemoryTemplateRepository>();
        }
        else
        {
            string connectionString = configuration.GetConnectionString("DocumentGenerator")
                ?? "Host=localhost;Port=5432;Database=document_generator;Username=document_generator;Password=document_generator";

            services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connectionString));
            services.AddDbContext<DocumentGeneratorDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<PostgresDocumentJobRepository>();
            services.AddScoped<IDocumentJobRepository>(providerServices =>
                providerServices.GetRequiredService<PostgresDocumentJobRepository>());
            services.AddScoped<IDocumentJobClaimer>(providerServices =>
                providerServices.GetRequiredService<PostgresDocumentJobRepository>());
            services.AddSingleton<IDocumentQueue, PostgresDocumentQueue>();
            services.AddScoped<ITemplateRepository, PostgresTemplateRepository>();
            services.AddSingleton<PostgresMigrationRunner>();
        }

        services.AddScoped<SubmitDocumentGenerationHandler>();
        services.AddScoped<GetDocumentStatusHandler>();
        services.AddScoped<CreateDownloadUrlHandler>();
        services.AddScoped<ProcessDocumentGenerationHandler>();

        return services;
    }
}
