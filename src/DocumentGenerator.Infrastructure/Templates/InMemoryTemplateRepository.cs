using System.Collections.Concurrent;
using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Infrastructure.Templates;

public sealed class InMemoryTemplateRepository : ITemplateRepository
{
    private readonly ConcurrentDictionary<string, DocumentTemplate> _templates = new(StringComparer.Ordinal);

    public InMemoryTemplateRepository()
    {
        DocumentTemplate sample = new("sample", "v1", "placeholder", DateTimeOffset.UtcNow);
        _templates[BuildKey(sample.TemplateId, sample.Version)] = sample;
    }

    public Task<DocumentTemplate?> GetAsync(string templateId, string version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _templates.TryGetValue(BuildKey(templateId, version), out DocumentTemplate? template);
        return Task.FromResult(template);
    }

    private static string BuildKey(string templateId, string version) => string.Concat(templateId, ':', version);
}
