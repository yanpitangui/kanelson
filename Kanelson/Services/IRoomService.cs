using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface IRoomService
{
    Task<long> CreateRoom(Guid templateId, string roomName);
    Task<ImmutableArray<RoomSummary>> GetAll();
    Task<RoomSummary> Get(long roomId);
    Task<CurrentQuestionInfo> GetCurrentQuestion(long roomId);
    Task NextQuestion(long roomId);
    Task Start(long roomId);
    Task<string> GetOwner(long roomId);
    Task Delete(long roomId);
    Task Answer(long roomId, Guid alternativeId);
    Task<RoomStatus> GetCurrentState(long roomId);
    void UserDisconnected(string userId, string connectionId);
    void UserConnected(long roomId, string userId, string connectionId);
}