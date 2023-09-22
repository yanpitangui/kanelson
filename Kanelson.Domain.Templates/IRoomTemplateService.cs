using Kanelson.Domain.Templates.Models;
using System.Collections.Immutable;

namespace Kanelson.Domain.Templates;

public interface IRoomTemplateService
{
    Task UpsertTemplate(Template template);
    Task<ImmutableArray<TemplateSummary>> GetTemplates();
    
    Task<Template> GetTemplate(Guid id);
    void DeleteTemplate(Guid id);
}