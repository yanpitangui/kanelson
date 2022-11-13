using Kanelson.Contracts.Models;

namespace Kanelson.Contracts.Grains.Templates;

public interface ITemplateGrain : IGrainWithGuidKey
{
    public Task SetBase(Template template, string ownerId);
    Task<string> GetOwner();
    Task<TemplateSummary> GetSummary();
    Task<Template> Get();
    Task Delete();
}