using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface IRoomService
{
    Task<string> CreateRoom(Guid templateId, string roomName);
    Task<ImmutableArray<RoomSummary>> GetAll();
    Task<RoomSummary> Get(string roomId);
    Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId);
    Task NextQuestion(string roomId);
    Task Start(string roomId);
    Task Delete(string roomId);
    Task Answer(string roomId, Guid alternativeId);
    Task<RoomStatus> GetCurrentState(string roomId);
    void UserDisconnected(string userId, string connectionId);
    void UserConnected(string roomId, string userId, string connectionId);
}