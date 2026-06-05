using System.Text.Json;

namespace DocumentGenerator.Contracts.Documents;

public sealed record SubmitDocumentGenerationRequest(
    string TemplateId,
    string TemplateVersion,
    OutputFormatDto OutputFormat,
    JsonElement InputData);
