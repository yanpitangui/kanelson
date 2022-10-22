using Orleans;
using Shared.Models;

namespace Shared.Grains.Rooms;

public interface IRoomGrain : IGrainWithStringKey
{

    Task SetBase(string roomName, string owner, Template template);
    Task<RoomSummary> GetSummary();
    Task UpdateCurrentUsers(HashSet<UserInfo> users);
    Task<HashSet<UserInfo>> GetCurrentUsers();
    Task<TemplateQuestion> GetCurrentQuestion();
    Task<bool> IncrementQuestionIdx();
    Task<bool> Start();
    Task<string> GetOwner();
    Task Delete();
}