using Kanelson.Contracts.Models;

namespace Kanelson.Contracts.Grains.Rooms;

public interface IRoomGrain : IGrainWithStringKey
{

    Task SetBase(string roomName, string owner, Template template);
    Task<RoomSummary> GetSummary();
    Task UpdateCurrentUsers(HashSet<UserInfo> users);
    Task<HashSet<UserInfo>> GetCurrentUsers();
    Task<TemplateQuestion> GetCurrentQuestion();
    Task<bool> NextQuestion();
    Task<bool> Start();
    Task<string> GetOwner();
    Task Delete();
    Task Answer(string userId, string roomId, Guid answerId);
}