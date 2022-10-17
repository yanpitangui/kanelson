using Orleans;
using Shared.Models;

namespace Shared.Grains.Templates;

public interface ITemplateGrain : IGrainWithGuidKey
{
    public Task SetBase(Template template, string ownerId);
    Task<string> GetOwner();
    Task<TemplateSummary> GetSummary();
    Task<Template> Get();
    Task Delete();
}