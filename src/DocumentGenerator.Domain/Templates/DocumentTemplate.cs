namespace DocumentGenerator.Domain.Templates;

public sealed record DocumentTemplate(
    string TemplateId,
    string Version,
    string Renderer,
    DateTimeOffset RegisteredAt);
