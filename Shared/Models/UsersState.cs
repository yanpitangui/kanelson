namespace Shared.Models;

[Serializable]
public record UsersState
{
    public HashSet<UserInfo> Users { get; set; } = new();
}