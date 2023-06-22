using Kanelson.Models;

namespace Kanelson.Actors;

public record UserIndexState
{
    public HashSet<UserInfo> Users { get; set; } = new();
}