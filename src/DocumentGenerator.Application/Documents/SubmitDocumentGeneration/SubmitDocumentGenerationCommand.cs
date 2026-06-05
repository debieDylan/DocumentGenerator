using System.Text.Json;
using DocumentGenerator.Domain.Jobs;

namespace DocumentGenerator.Application.Documents.SubmitDocumentGeneration;

public sealed record SubmitDocumentGenerationCommand(
    string TemplateId,
    string TemplateVersion,
    OutputFormat OutputFormat,
    JsonElement InputData,
    string? IdempotencyKey);
