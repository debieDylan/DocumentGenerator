using DocumentGenerator.Application.Abstractions;
using DocumentGenerator.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerator.Infrastructure.Persistence.Templates;

public sealed class PostgresTemplateRepository(DocumentGeneratorDbContext dbContext) : ITemplateRepository
{
    public async Task<DocumentTemplate?> GetAsync(string templateId, string version, CancellationToken cancellationToken)
    {
        Models.DocumentTemplateEntity? template = await dbContext.Templates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.TemplateId == templateId && candidate.Version == version,
                cancellationToken);

        return template is null
            ? null
            : new DocumentTemplate(template.TemplateId, template.Version, template.Renderer, template.RegisteredAt);
    }
}
