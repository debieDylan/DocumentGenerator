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

`DocumentGenerator.Infrastructure` owns adapter implementations. The current implementation is in-memory and exists only so the skeleton compiles. Future production adapters should replace it with PostgreSQL metadata storage, a durable queue, blob/object storage, and real renderers.

`DocumentGenerator.Contracts` contains public DTOs for HTTP and future queue contracts.

## Current Storage Model

PostgreSQL is intended to be the source of truth for metadata and job status. Blob/object storage is intended to be the source of truth for generated files. Large file bytes should not be stored in PostgreSQL.

The current `InMemory*` infrastructure is a placeholder only. It is not suitable for multiple API or worker instances.

## API Shape

```http
POST /documents
GET /documents/{id}
GET /documents/{id}/download-url
GET /health
```

`POST /documents` accepts an optional `Idempotency-Key` header. Same key plus same payload returns the existing job. Same key plus a different payload returns a conflict.

The placeholder template repository includes one template: `sample:v1`.

## Development

```powershell
dotnet build DocumentGenerator.slnx
dotnet run --project src/DocumentGenerator.Api/DocumentGenerator.Api.csproj
dotnet run --project src/DocumentGenerator.Worker/DocumentGenerator.Worker.csproj
```

The first production-oriented work should be to replace the in-memory adapters with PostgreSQL, queue, blob storage, and renderer implementations while keeping the application ports stable.
