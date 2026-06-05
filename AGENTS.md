\# AGENTS.md



\# Purpose



This repository contains a distributed document generation platform built on .NET.



The platform is responsible for:



\* Accepting document generation requests.

\* Managing document generation jobs.

\* Rendering document artifacts.

\* Storing generated artifacts.

\* Exposing job status and download capabilities.



The platform is intentionally \*\*business-domain agnostic\*\*.



It should not know whether it is generating:



\* Invoices

\* Reports

\* Search exports

\* Statements

\* Contracts



The platform receives data and rendering instructions and produces artifacts.



\---



\# Core Architectural Principles



The system is built around the following principles:



\* Vertical Slice Architecture

\* Ports and Adapters

\* Async-first design

\* Distributed-system-first design

\* Stateless processing

\* Queue-based workloads

\* Blob storage for artifacts

\* PostgreSQL for metadata

\* Template versioning

\* Idempotent operations

\* Explicit error handling through Result/Error/Option primitives



\---



\# High-Level Architecture



```text

Calling Application

&#x20;       |

&#x20;       v

DocumentGenerator.Api

&#x20;       |

&#x20;       | Create Job

&#x20;       v

PostgreSQL

&#x20;       |

&#x20;       | Queue Work

&#x20;       v

Message Queue

&#x20;       |

&#x20;       v

DocumentGenerator.Worker

&#x20;       |

&#x20;       | Render Artifact

&#x20;       | Upload Artifact

&#x20;       | Update Status

&#x20;       v

Blob Storage

```



The API and Worker are separate deployable units.



They must be able to scale independently.



\---



\# Solution Structure



```text

src/



DocumentGenerator.Api

DocumentGenerator.Worker



DocumentGenerator.Application

DocumentGenerator.Domain

DocumentGenerator.Infrastructure

DocumentGenerator.Contracts



tests/



DocumentGenerator.UnitTests

DocumentGenerator.IntegrationTests

DocumentGenerator.ArchitectureTests

```



\---



\# Architectural Style



Use Vertical Slice Architecture.



Organize Application code by feature.



Good:



```text

Application



Documents

&#x20;├── SubmitDocumentGeneration

&#x20;├── GetDocumentStatus

&#x20;└── CreateDownloadUrl



Templates

&#x20;├── RegisterTemplate

&#x20;└── GetTemplate

```



Avoid:



```text

Services

Repositories

Managers

Helpers

Utilities

```



Large generic folders become difficult to maintain.



\---



\# Dependency Direction



Allowed:



```text

Api -> Application

Worker -> Application



Infrastructure -> Application

Infrastructure -> Domain



Application -> Domain

```



Forbidden:



```text

Domain -> Application

Domain -> Infrastructure



Application -> Infrastructure

```



The Domain layer must never depend on infrastructure concerns.



\---



\# Responsibilities



\## Api



Responsible for:



\* HTTP endpoints

\* Authentication

\* Authorization

\* Validation

\* Invoking application use cases



The API should remain thin.



Business logic does not belong in controllers.



\---



\## Worker



Responsible for:



\* Consuming jobs

\* Executing use cases

\* Updating status

\* Logging failures



Workers should remain thin.



Workers should not contain business logic.



\---



\## Application



Responsible for:



\* Use cases

\* Workflow orchestration

\* Validation rules

\* Interfaces (ports)



Application owns the process.



\---



\## Domain



Responsible for:



\* Core models

\* State transitions

\* Value objects

\* Domain rules



The domain should remain pragmatic.



Avoid excessive DDD complexity.



\---



\## Infrastructure



Responsible for:



\* PostgreSQL

\* Blob Storage

\* Queue integrations

\* Template storage

\* Renderers

\* External services



Infrastructure owns the tools.



\---



\## Contracts



Responsible for:



\* Public DTOs

\* API contracts

\* Queue messages



Contracts should be stable and versioned.



\---



\# Distributed System Principles



Assume at all times:



\* Multiple API instances may exist.

\* Multiple Worker instances may exist.

\* Instances may be terminated at any time.

\* Requests may hit any API instance.

\* Jobs may be processed by any Worker instance.



The system must behave correctly regardless of instance count.



Correctness must never depend on:



\* Local memory

\* Static dictionaries

\* Singleton state

\* Local disk

\* Sticky sessions



All durable state must live in shared infrastructure.



\---



\# State Management



Allowed durable storage:



\* PostgreSQL

\* Blob Storage

\* Message Queue

\* Distributed Cache (optional)



Forbidden:



```csharp

static Dictionary<Guid, JobStatus>

```



```csharp

ConcurrentDictionary<Guid, JobStatus>

```



```csharp

MemoryCache as source of truth

```



Memory is an optimization only.



Never a source of truth.



\---



\# Job Processing



Workers must be idempotent.



A job may be:



\* Delivered multiple times

\* Retried

\* Replayed

\* Reprocessed after crashes



Processing the same job twice must not corrupt state.



Design all processing with retries in mind.



\---



\# Locking



Prefer:



\* Optimistic concurrency

\* Database constraints

\* Unique indexes



Avoid distributed locks unless absolutely necessary.



Distributed locks should be the exception, not the default solution.



\---



\# Templates



Templates are first-class concepts.



Templates must always be versioned.



Examples:



```text

invoice:v1

invoice:v2

invoice:v3

```



Never overwrite historical template versions.



Jobs must always reference the exact template version used.



Benefits:



\* Reproducibility

\* Auditing

\* Traceability



\---



\# Storage Rules



\## PostgreSQL



PostgreSQL is the source of truth.



Store:



\* Jobs

\* Artifacts metadata

\* Templates metadata

\* Idempotency records



Do not store large files in PostgreSQL.



\---



\## Blob Storage



Blob storage is the source of truth for generated artifacts.



Store:



\* PDFs

\* CSVs

\* XLSX files

\* ZIP files



Only references belong in PostgreSQL.



\---



\# Artifact Downloads



Artifacts should be downloaded through signed URLs.



Avoid proxying large files through the API.



Preferred flow:



```text

Client

&#x20; -> API



API

&#x20; -> Generates signed URL



Client

&#x20; -> Blob Storage

```



\---



\# Communication Pattern



Default integration pattern:



```text

POST /documents

GET /documents/{id}

GET /documents/{id}/download-url

```



V1 should use polling.



Future additions may include:



\* Webhooks

\* Domain events

\* Kafka

\* SignalR dashboards



Polling remains the default integration model.



\---



\# Idempotency



Idempotency is required.



Every document request should support an idempotency key.



Example:



```http

Idempotency-Key: abc123

```



Same key + same payload:



```text

Return existing job.

```



Same key + different payload:



```text

409 Conflict.

```



Idempotency prevents duplicate work.



\---



\# Caching



Caching is optional.



Caching must never affect correctness.



The system must continue functioning when all caches are empty.



A cache key should include:



\* Tenant

\* Template Id

\* Template Version

\* Output Format

\* Input Data Hash

\* Renderer Version



Never cache solely on request payload.



\---



\# Functional Primitives



Prefer explicit outcomes.



Use:



```text

Result

Result<T>

Error

Option<T>

```



Expected failures should be modeled explicitly.



Good:



```csharp

Result<DocumentJob>

Result<Uri>

Option<DocumentArtifact>

```



Avoid:



```csharp

throw new NotFoundException()

throw new ValidationException()

```



for expected outcomes.



Exceptions are reserved for:



\* Infrastructure failures

\* External service failures

\* Corruption

\* Programming errors



\---



\# Async Rules



The platform is async-first.



Use:



```csharp

Task

ValueTask

IAsyncEnumerable

```



when appropriate.



All asynchronous operations must accept:



```csharp

CancellationToken

```



Do not ignore cancellation requests.



\---



\# Performance Principles



Performance is a first-class concern.



Prefer:



\* Streaming

\* Batching

\* Minimal allocations

\* Async APIs



Avoid:



\* Reflection-heavy designs

\* Dynamic dispatch

\* Large temporary buffers

\* Excessive LINQ in hot paths



Measure before optimizing.



Design for scale from the beginning.



\---



\# Large File Handling



Assume large artifacts are possible.



Examples:



\* Large PDFs

\* Large CSV exports

\* Multi-thousand-page reports



Avoid:



```csharp

byte\[]

MemoryStream containing entire document

```



when possible.



Prefer:



\* Streaming

\* Incremental processing

\* Direct upload streams



Workers must remain stable under concurrent large-document generation.



\---



\# Security Principles



Design for:



\* OAuth2

\* JWT authentication

\* Tenant isolation

\* Signed download URLs

\* Input validation

\* Request size limits



Never trust client input.



Never commit secrets.



Never expose storage credentials.



\---



\# Observability



Observability is required.



Support:



\* Structured logging (Serilog)

\* Correlation IDs

\* Metrics (OpenTelemetry)

\* Tracing (OpenTelemetry)

\* Health checks

\* Readiness checks



Track:



\* Queue depth

\* Job duration

\* Render duration

\* Upload duration

\* Failure rate

\* Retry count

\* Worker throughput



\---



\# Abstraction Rules



Do not create abstractions "just in case".



Create interfaces when:



\* Multiple implementations are expected.

\* Infrastructure must be isolated.

\* Testing benefits significantly.



Avoid:



```text

IRepository<T>

IManager

IService

```



Prefer:



```text

IDocumentJobRepository

IDocumentStorage

IDocumentQueue

ITemplateRepository

IDocumentRenderer

```



Use specific abstractions.



\---



\# Coding Guidelines



Use:



\* Nullable reference types

\* File-scoped namespaces

\* Primary constructors where appropriate

\* Sealed classes by default

\* Immutable models where practical



Favor readability.



Favor correctness.



Favor explicitness.



\---



\# Public Repository Rules



This repository is public.



Never commit:



\* Secrets

\* API keys

\* Certificates

\* Customer data

\* Generated artifacts

\* Local environment files



Use sample configuration only.



\---



\# Final Rule



When making architectural decisions, optimize for:



1\. Correctness

2\. Simplicity

3\. Scalability

4\. Maintainability

5\. Performance



Never sacrifice correctness for performance.



Never sacrifice maintainability for cleverness.



