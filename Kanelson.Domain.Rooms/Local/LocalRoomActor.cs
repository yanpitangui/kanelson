using Akka.Actor;
using Kanelson.Domain.Users;
using System.Threading.Channels;

namespace Kanelson.Domain.Rooms.Local;

public sealed class LocalRoomActor : ReceiveActor
{
    private readonly record struct Subscriber(string UserId, string UserName, ChannelWriter<IRoomEvent> Writer);

    private readonly string _roomId;
    private readonly IActorRef _roomShard;
    private readonly Dictionary<Guid, Subscriber> _subscribers = new();

    public LocalRoomActor(string roomId, IActorRef roomShard)
    {
        _roomId = roomId;
        _roomShard = roomShard;
        SetReceiveTimeout(TimeSpan.FromMinutes(5));

        Receive<SubscribeToRoom>(HandleSubscribe);
        Receive<UnsubscribeFromRoom>(HandleUnsubscribe);
        Receive<IWithRoomId>(msg => _roomShard.Forward(msg));
        Receive<BroadcastEvent>(o => FanOut(o.Event, userId: null));
        Receive<SendToUser>(o => FanOut(o.Event, o.UserId));
        Receive<ReceiveTimeout>(_ => Context.Stop(Self));
    }

    private void HandleSubscribe(SubscribeToRoom msg)
    {
        var channel = Channel.CreateBounded<IRoomEvent>(new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        var id = Guid.NewGuid();
        _subscribers[id] = new Subscriber(msg.UserId, msg.UserName, channel.Writer);
        SetReceiveTimeout(null);
        NotifyRoomUsers();
        _roomShard.Tell(new RoomCommands.UserConnected(_roomId, msg.UserId));
        Sender.Tell(new SubscriptionResult(id, channel.Reader));
    }

    private void HandleUnsubscribe(UnsubscribeFromRoom msg)
    {
        if (!_subscribers.TryGetValue(msg.SubscriptionId, out var sub)) return;
        _subscribers.Remove(msg.SubscriptionId);
        sub.Writer.TryComplete();
        NotifyRoomUsers();
        if (_subscribers.Count == 0)
            SetReceiveTimeout(TimeSpan.FromMinutes(5));
    }

    private void FanOut(IRoomEvent evt, string? userId)
    {
        foreach (var sub in _subscribers.Values)
        {
            if (userId is null || string.Equals(sub.UserId, userId, StringComparison.OrdinalIgnoreCase))
                sub.Writer.TryWrite(evt);
        }
    }

    private void NotifyRoomUsers()
    {
        var users = _subscribers.Values
            .DistinctBy(s => s.UserId, StringComparer.OrdinalIgnoreCase)
            .Select(s => new UserInfo { Id = s.UserId, Name = s.UserName })
            .ToHashSet();
        _roomShard.Tell(new RoomCommands.UpdateCurrentUsers(_roomId, users));
    }

    protected override void PostStop()
    {
        foreach (var sub in _subscribers.Values)
            sub.Writer.TryComplete();
    }

    public static Props Props(string roomId, IActorRef roomShard) =>
        Akka.Actor.Props.Create<LocalRoomActor>(roomId, roomShard);
}
