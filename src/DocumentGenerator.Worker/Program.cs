using DocumentGenerator.Worker;
using DocumentGenerator.Infrastructure.DependencyInjection;
using DocumentGenerator.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.AddHealthChecks();
builder.Services.AddDocumentGenerator(builder.Configuration);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await host.Services.GetRequiredService<PostgresMigrationRunner>().ApplyAsync(CancellationToken.None);
    return;
}

await host.RunAsync();
