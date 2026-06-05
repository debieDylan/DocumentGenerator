using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Artifacts;
using DocumentGenerator.Domain.Idempotency;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Infrastructure.Jobs;
using Xunit;

namespace DocumentGenerator.Tests;

public sealed class DocumentJobClaimingTests
{
    [Fact]
    public async Task Claiming_pending_job_succeeds()
    {
        InMemoryDocumentJobRepository repository = new();
        DocumentJob job = CreateJob();
        await repository.SubmitAsync(job, CreateIdempotency(job), CancellationToken.None);

        Result<DocumentJob> claim = await repository.TryClaimJobAsync(
            job.Id,
            "worker-1",
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        Assert.True(claim.IsSuccess);
        Assert.Equal("worker-1", claim.Value!.ClaimedBy);
        Assert.Equal(DocumentJobStatus.Processing, claim.Value.Status);
    }

    [Fact]
    public async Task Claiming_already_claimed_non_expired_job_fails()
    {
        InMemoryDocumentJobRepository repository = new();
        DocumentJob job = CreateJob();
        await repository.SubmitAsync(job, CreateIdempotency(job), CancellationToken.None);
        await repository.TryClaimJobAsync(job.Id, "worker-1", DateTimeOffset.UtcNow.AddMinutes(5), CancellationToken.None);

        Result<DocumentJob> secondClaim = await repository.TryClaimJobAsync(
            job.Id,
            "worker-2",
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        Assert.True(secondClaim.IsFailure);
        Assert.Equal("job.claim_failed", secondClaim.Error!.Code);
    }

    [Fact]
    public async Task Claiming_completed_job_fails()
    {
        InMemoryDocumentJobRepository repository = new();
        DocumentJob job = CreateJob();
        await repository.SubmitAsync(job, CreateIdempotency(job), CancellationToken.None);
        DocumentArtifact artifact = new(
            DocumentArtifactId.New(),
            job.Id,
            new Uri("memory://documents/test.pdf"),
            OutputFormat.Pdf,
            10,
            "application/pdf",
            DateTimeOffset.UtcNow);
        job.MarkCompleted(artifact, DateTimeOffset.UtcNow);
        await repository.UpdateAsync(job, CancellationToken.None);

        Result<DocumentJob> claim = await repository.TryClaimJobAsync(
            job.Id,
            "worker-1",
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        Assert.True(claim.IsFailure);
        Assert.Equal("job.claim_failed", claim.Error!.Code);
    }

    [Fact]
    public async Task Expired_claim_can_be_claimed_again()
    {
        InMemoryDocumentJobRepository repository = new();
        DocumentJob job = CreateJob();
        await repository.SubmitAsync(job, CreateIdempotency(job), CancellationToken.None);
        await repository.TryClaimJobAsync(job.Id, "worker-1", DateTimeOffset.UtcNow.AddMinutes(-1), CancellationToken.None);

        Result<DocumentJob> secondClaim = await repository.TryClaimJobAsync(
            job.Id,
            "worker-2",
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        Assert.True(secondClaim.IsSuccess);
        Assert.Equal("worker-2", secondClaim.Value!.ClaimedBy);
    }

    private static DocumentJob CreateJob() =>
        new(
            DocumentJobId.New(),
            "sample",
            "v1",
            OutputFormat.Pdf,
            "payload-hash",
            "key-1",
            DateTimeOffset.UtcNow);

    private static IdempotencyRecord CreateIdempotency(DocumentJob job) =>
        new("key-1", job.InputDataHash, job.Id, DateTimeOffset.UtcNow);
}
