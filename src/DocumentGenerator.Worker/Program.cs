using DocumentGenerator.Worker;
using DocumentGenerator.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.AddHealthChecks();
builder.Services.AddDocumentGenerator();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
