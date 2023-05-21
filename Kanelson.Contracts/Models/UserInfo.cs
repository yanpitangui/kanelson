namespace Kanelson.Contracts.Models;

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

    public string Id { get; init; } = null!;
    
    public string Name { get; init; } = null!;
}