using Kanelson.Actors.Rooms;
using System.Collections.Immutable;
using Kanelson.Models;

namespace Kanelson.Services;

public interface IRoomService
{
    Task<string> CreateRoom(Guid templateId, string roomName, CancellationToken ct = default);
    Task<ImmutableArray<BasicRoomInfo>> GetAll(CancellationToken ct = default);
    Task<RoomSummary> Get(string roomId, CancellationToken ct = default);
    Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId, CancellationToken ct = default);
    Task NextQuestion(string roomId, CancellationToken ct = default);
    Task Start(string roomId, CancellationToken ct = default);
    Task Delete(string roomId, CancellationToken ct = default);
    Task Answer(string roomId, Guid alternativeId, CancellationToken ct = default);
    Task<RoomStatus> GetCurrentState(string roomId, CancellationToken ct = default);
    void UserDisconnected(string userId, string connectionId);
    void UserConnected(string roomId, string userId, string connectionId);
}