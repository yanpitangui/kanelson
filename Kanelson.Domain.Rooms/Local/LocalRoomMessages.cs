using System.Threading.Channels;

namespace Kanelson.Domain.Rooms.Local;

public record GetLocalRoom(string RoomId);
public record SubscribeToRoom(string RoomId, string UserId, string UserName);
public record UnsubscribeFromRoom(string RoomId, Guid SubscriptionId);
public record SubscriptionResult(Guid SubscriptionId, ChannelReader<IRoomEvent> Reader);
public record BroadcastEvent(string RoomId, IRoomEvent Event);
public record SendToUser(string RoomId, string UserId, IRoomEvent Event);
