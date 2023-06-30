using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface ITemplateService
{
    Task UpsertTemplate(Template template);
    Task<ImmutableArray<TemplateSummary>> GetTemplates();
    
    Task<Template> GetTemplate(Guid id);
    Task DeleteTemplate(Guid id);
}