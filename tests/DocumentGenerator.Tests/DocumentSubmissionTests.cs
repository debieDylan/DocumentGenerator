using System.Text.Json;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.SubmitDocumentGeneration;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Domain.Templates;
using DocumentGenerator.Infrastructure.Jobs;
using Xunit;

namespace DocumentGenerator.Tests;

public sealed class DocumentSubmissionTests
{
    [Fact]
    public async Task Submit_new_document_creates_job_and_idempotency_record()
    {
        TestContext context = CreateContext();
        SubmitDocumentGenerationCommand command = CreateCommand("key-1", """{"name":"first"}""");

        Result<SubmitDocumentGenerationResponse> result = await context.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.ReusedExistingJob);
        Assert.Equal(DocumentJobStatus.Pending, result.Value.Status);
        Assert.Single(context.Queue.EnqueuedJobs);
    }

    [Fact]
    public async Task Submit_same_idempotency_key_and_payload_returns_existing_job()
    {
        TestContext context = CreateContext();
        SubmitDocumentGenerationCommand command = CreateCommand("key-1", """{"name":"first"}""");

        Result<SubmitDocumentGenerationResponse> first = await context.Handler.HandleAsync(command, CancellationToken.None);
        Result<SubmitDocumentGenerationResponse> second = await context.Handler.HandleAsync(command, CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.True(second.Value!.ReusedExistingJob);
        Assert.Equal(first.Value!.JobId, second.Value.JobId);
        Assert.Single(context.Queue.EnqueuedJobs);
    }

    [Fact]
    public async Task Submit_same_idempotency_key_and_different_payload_returns_conflict()
    {
        TestContext context = CreateContext();

        await context.Handler.HandleAsync(CreateCommand("key-1", """{"name":"first"}"""), CancellationToken.None);
        Result<SubmitDocumentGenerationResponse> conflict = await context.Handler.HandleAsync(
            CreateCommand("key-1", """{"name":"second"}"""),
            CancellationToken.None);

        Assert.True(conflict.IsFailure);
        Assert.Equal("idempotency.conflict", conflict.Error!.Code);
    }

    private static TestContext CreateContext()
    {
        InMemoryDocumentJobRepository jobs = new();
        TestTemplateRepository templates = new();
        TestDocumentQueue queue = new();
        TestClock clock = new(new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero));
        SubmitDocumentGenerationHandler handler = new(jobs, templates, queue, clock);
        return new TestContext(handler, queue);
    }

    private static SubmitDocumentGenerationCommand CreateCommand(string key, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new SubmitDocumentGenerationCommand(
            "sample",
            "v1",
            OutputFormat.Pdf,
            document.RootElement.Clone(),
            key);
    }

    private sealed record TestContext(SubmitDocumentGenerationHandler Handler, TestDocumentQueue Queue);

    private sealed class TestTemplateRepository : ITemplateRepository
    {
        public Task<DocumentTemplate?> GetAsync(string templateId, string version, CancellationToken cancellationToken)
        {
            DocumentTemplate template = new(templateId, version, "placeholder", DateTimeOffset.UtcNow);
            return Task.FromResult<DocumentTemplate?>(template);
        }
    }

    private sealed class TestDocumentQueue : IDocumentQueue
    {
        public List<DocumentJobId> EnqueuedJobs { get; } = [];

        public Task EnqueueAsync(DocumentJobId jobId, CancellationToken cancellationToken)
        {
            EnqueuedJobs.Add(jobId);
            return Task.CompletedTask;
        }

        public ValueTask<DocumentJobId?> DequeueAsync(CancellationToken cancellationToken) => ValueTask.FromResult<DocumentJobId?>(null);
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
