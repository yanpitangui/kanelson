using Kanelson.Contracts.Models;

namespace Kanelson.Grains;

public record UserIndexState
{
    public HashSet<UserInfo> Users { get; set; } = new();
}