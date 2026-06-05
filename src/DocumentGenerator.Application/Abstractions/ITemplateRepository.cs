using DocumentGenerator.Domain.Templates;

namespace DocumentGenerator.Application.Abstractions;

public interface ITemplateRepository
{
    Task<DocumentTemplate?> GetAsync(string templateId, string version, CancellationToken cancellationToken);
}
