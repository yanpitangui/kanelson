namespace Kanelson.Contracts.Models;

[GenerateSerializer]
public record UserInfo
{
    protected UserInfo()
    {
        
    }
    public UserInfo(string id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Id(0)]
    public string Id { get; init; } = null!;
    
    [Id(1)]
    public string Name { get; init; } = null!;
}