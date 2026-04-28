using Kanelson.Domain.Rooms.Models;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace Kanelson.Domain.Rooms;

public interface IRoomService
{
    Task<string> CreateRoom(Guid templateId, string roomName, CancellationToken ct = default);
    Task<ChannelReader<ImmutableArray<BasicRoomInfo>>> GetRoomsChannel(CancellationToken ct = default);
    Task<RoomSummary> Get(string roomId, CancellationToken ct = default);
    Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId, CancellationToken ct = default);
    Task NextQuestion(string roomId, CancellationToken ct = default);
    Task Start(string roomId, CancellationToken ct = default);
    Task Delete(string roomId, CancellationToken ct = default);
    Task Answer(string roomId, CancellationToken ct = default, params Guid[] alternativeIds);
    Task<RoomStatus> GetCurrentState(string roomId, CancellationToken ct = default);
}
