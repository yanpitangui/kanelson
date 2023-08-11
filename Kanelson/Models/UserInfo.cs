namespace Kanelson.Models;

public record UserInfo
{
    protected UserInfo()
    {
        
    }
    public UserInfo(string id)
    {
        Id = id;
    }
    public string Id { get; init; } = null!;
    
    public string Name { get; set; } = null!;
}