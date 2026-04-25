using Akka.Actor;

namespace Kanelson.Domain.Rooms.Local;

public sealed class LocalRoomActorManager : ReceiveActor
{
    private readonly IActorRef _roomShard;

    public LocalRoomActorManager(IActorRef roomShard)
    {
        _roomShard = roomShard;

        Receive<GetLocalRoom>(msg => Sender.Tell(GetOrCreateRoom(msg.RoomId)));
        Receive<SubscribeToRoom>(msg => GetOrCreateRoom(msg.RoomId).Forward(msg));
        Receive<UnsubscribeFromRoom>(msg => GetOrCreateRoom(msg.RoomId).Forward(msg));
        Receive<BroadcastEvent>(msg => GetOrCreateRoom(msg.RoomId).Forward(msg));
        Receive<SendToUser>(msg => GetOrCreateRoom(msg.RoomId).Forward(msg));
    }

    private IActorRef GetOrCreateRoom(string roomId)
    {
        var child = Context.Child(roomId);
        if (!child.IsNobody()) return child;
        return Context.ActorOf(LocalRoomActor.Props(roomId, _roomShard), roomId);
    }

    public static Props Props(IActorRef roomShard) =>
        Akka.Actor.Props.Create<LocalRoomActorManager>(roomShard);
}
