namespace DocumentGenerator.Infrastructure.Persistence.Models;

public sealed class DocumentTemplateEntity
{
    public string TemplateId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Renderer { get; set; } = string.Empty;

    public DateTimeOffset RegisteredAt { get; set; }
}
