using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Application.Documents.CreateDownloadUrl;
using DocumentGenerator.Application.Documents.GetDocumentStatus;
using DocumentGenerator.Application.Documents.SubmitDocumentGeneration;
using DocumentGenerator.Contracts.Documents;
using DocumentGenerator.Domain.Jobs;
using DocumentGenerator.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDocumentGenerator();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

RouteGroupBuilder documents = app.MapGroup("/documents");

documents.MapPost(
    "/",
    async (
        SubmitDocumentGenerationRequest request,
        SubmitDocumentGenerationHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        string? idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
        SubmitDocumentGenerationCommand command = new(
            request.TemplateId,
            request.TemplateVersion,
            MapOutputFormat(request.OutputFormat),
            request.InputData,
            string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey);

        Result<SubmitDocumentGenerationResponse> result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(result.Error);
        }

        SubmitDocumentGenerationResponse response = result.Value!;
        return Results.Accepted(
            $"/documents/{response.JobId.Value}",
            new DocumentJobResponse(response.JobId.Value, response.Status.ToString(), response.ReusedExistingJob));
    });

documents.MapGet(
    "/{jobId:guid}",
    async (Guid jobId, GetDocumentStatusHandler handler, CancellationToken cancellationToken) =>
    {
        Result<GetDocumentStatusResponse> result = await handler.HandleAsync(
            new GetDocumentStatusQuery(new DocumentJobId(jobId)),
            cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(result.Error);
        }

        GetDocumentStatusResponse response = result.Value!;
        return Results.Ok(new DocumentStatusResponse(
            response.JobId.Value,
            response.Status.ToString(),
            response.TemplateId,
            response.TemplateVersion,
            response.OutputFormat.ToString(),
            response.ArtifactUri,
            response.FailureReason,
            response.CreatedAt,
            response.StartedAt,
            response.CompletedAt));
    });

documents.MapGet(
    "/{jobId:guid}/download-url",
    async (Guid jobId, CreateDownloadUrlHandler handler, CancellationToken cancellationToken) =>
    {
        Result<CreateDownloadUrlResponse> result = await handler.HandleAsync(
            new CreateDownloadUrlQuery(new DocumentJobId(jobId), TimeSpan.FromMinutes(15)),
            cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(result.Error);
        }

        CreateDownloadUrlResponse response = result.Value!;
        return Results.Ok(new DownloadUrlResponse(response.Url, response.ExpiresAt));
    });

app.Run();

static OutputFormat MapOutputFormat(OutputFormatDto format) =>
    format switch
    {
        OutputFormatDto.Pdf => OutputFormat.Pdf,
        OutputFormatDto.Csv => OutputFormat.Csv,
        OutputFormatDto.Xlsx => OutputFormat.Xlsx,
        OutputFormatDto.Zip => OutputFormat.Zip,
        _ => OutputFormat.Pdf,
    };

static IResult MapFailure(Error? error) =>
    error?.Code switch
    {
        "idempotency.conflict" => Results.Conflict(error),
        "job.not_found" or "template.not_found" => Results.NotFound(error),
        "artifact.not_ready" => Results.BadRequest(error),
        _ => Results.Problem(error?.Message ?? "An unexpected error occurred."),
    };
