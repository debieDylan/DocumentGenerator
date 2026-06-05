# DocumentGenerator

DocumentGenerator is a scalable, business-domain agnostic document generation platform skeleton for .NET.

The service accepts document generation requests, stores job metadata, queues work, lets workers render artifacts, and returns signed download URLs once artifacts are available. It intentionally does not know whether callers are generating invoices, reports, statements, exports, contracts, or any other business-specific document.

## Architecture

```text
Calling Application
        |
        v
DocumentGenerator.Api
        |
        | Create Job
        v
PostgreSQL
        |
        | Queue Work
        v
Message Queue
        |
        v
DocumentGenerator.Worker
        |
        | Render Artifact
        | Upload Artifact
        | Update Status
        v
Blob Storage
```

## Projects

```text
src/
  DocumentGenerator.Api/
  DocumentGenerator.Worker/
  DocumentGenerator.Application/
  DocumentGenerator.Domain/
  DocumentGenerator.Infrastructure/
  DocumentGenerator.Contracts/

tests/
  DocumentGenerator.Tests/
```

## Boundaries

`DocumentGenerator.Api` is a thin HTTP host. It maps public contracts to application use cases and exposes health check placeholders.

`DocumentGenerator.Worker` is a thin background host. It consumes queued job IDs and invokes application workflows.

`DocumentGenerator.Application` owns the vertical slices:

- `SubmitDocumentGeneration`
- `GetDocumentStatus`
- `CreateDownloadUrl`
- `ProcessDocumentGeneration`

`DocumentGenerator.Domain` contains lightweight concepts such as `DocumentJob`, `DocumentArtifact`, `DocumentTemplate`, `IdempotencyRecord`, `DocumentJobStatus`, and `OutputFormat`.

`DocumentGenerator.Infrastructure` owns adapter implementations. PostgreSQL is the default metadata provider. In-memory implementations still exist for explicit development or unit-test use, but they are not the production default.

`DocumentGenerator.Contracts` contains public DTOs for HTTP and future queue contracts.

## Current Storage Model

PostgreSQL is the source of truth for metadata and job status. Blob/object storage is intended to be the source of truth for generated files. Large file bytes should not be stored in PostgreSQL.

The metadata schema is managed by SQL migration files in `DocumentGenerator.Infrastructure/Persistence/Migrations` and currently includes:

- `document_jobs`
- `document_artifacts`
- `document_templates`
- `idempotency_records`

The current renderer and storage adapters are still placeholders. Real PDF rendering and blob/object storage are intentionally not implemented yet.

## Idempotency

`POST /documents` accepts an optional `Idempotency-Key` header.

Submission is handled through an atomic application port implemented by PostgreSQL transactions and constraints:

- New key: create an idempotency record, create the job, link the two, enqueue work, and return the new job id.
- Same key and same request hash: return the existing job id.
- Same key and different request hash: return a conflict.

This behavior does not depend on local memory or process-level locks.

## Job Claiming

Workers do not directly mark a job as processing with local state. The worker asks the application workflow to claim a job, and Infrastructure performs an atomic PostgreSQL `UPDATE ... RETURNING`.

A job can be claimed when:

- It is `Pending`.
- It is `Processing` but the previous claim has expired.

A job cannot be claimed when:

- It is already claimed and the claim has not expired.
- It is `Completed`.
- It is `Failed`.

The claim stores `claimed_by`, `claim_expires_at`, `started_at`, `updated_at`, and increments `retry_count`.

## API Shape

```http
POST /documents
GET /documents/{id}
GET /documents/{id}/download-url
GET /health
```

`POST /documents` accepts an optional `Idempotency-Key` header. Same key plus same payload returns the existing job. Same key plus a different payload returns a conflict.

The initial migration inserts one sample template: `sample:v1`.

## Development

Start local PostgreSQL:

```powershell
docker compose up -d postgres
```

Apply SQL migrations:

```powershell
dotnet run --project src/DocumentGenerator.Api/DocumentGenerator.Api.csproj -- --migrate
```

Build and test:

```powershell
dotnet build DocumentGenerator.slnx
dotnet test DocumentGenerator.slnx
dotnet run --project src/DocumentGenerator.Api/DocumentGenerator.Api.csproj
dotnet run --project src/DocumentGenerator.Worker/DocumentGenerator.Worker.csproj
```

Default local configuration uses:

```text
Host=localhost;Port=5432;Database=document_generator;Username=document_generator;Password=document_generator
```

These credentials are for local development only. Override `ConnectionStrings:DocumentGenerator` through environment-specific configuration for real deployments.

To force in-memory metadata for tests or local experiments, set:

```json
{
  "DocumentGenerator": {
    "MetadataProvider": "InMemory"
  }
}
```

The unit tests cover application submission and claim behavior. PostgreSQL concurrency integration tests should be added next with a real PostgreSQL container to prove concurrent idempotency and worker claiming under database load.
