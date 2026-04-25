using Akka.Actor;
using System.Collections.Immutable;
using System.Threading.Channels;

namespace Kanelson.Domain.Rooms;

public sealed class AllRoomsIndexActor : ReceiveActor
{
    private readonly Dictionary<string, BasicRoomInfo> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ChannelWriter<ImmutableArray<BasicRoomInfo>>> _writers = [];

    public AllRoomsIndexActor()
    {
        Receive<AllRoomsPublisherMessages.RoomRegistered>(r =>
        {
            _rooms[r.RoomId] = new BasicRoomInfo(r.RoomId, r.RoomName, r.OwnerId);
            FanOut();
        });

        Receive<AllRoomsPublisherMessages.RoomUnregistered>(r =>
        {
            _rooms.Remove(r.RoomId);
            FanOut();
        });

        Receive<AllRoomsIndexMessages.GetRoomsReader>(_ =>
        {
            var channel = Channel.CreateBounded<ImmutableArray<BasicRoomInfo>>(
                new BoundedChannelOptions(10)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true
                });
            _writers.Add(channel.Writer);
            channel.Writer.TryWrite(Snapshot());
            Sender.Tell(channel.Reader);
        });

        Receive<AllRoomsIndexMessages.CheckRoomExists>(msg =>
        {
            Sender.Tell(_rooms.ContainsKey(msg.RoomId));
        });
    }

    private ImmutableArray<BasicRoomInfo> Snapshot() =>
        _rooms.Values.ToImmutableArray();

    private void FanOut()
    {
        var snapshot = Snapshot();
        _writers.RemoveAll(w =>
        {
            try { return !w.TryWrite(snapshot); }
            catch { return true; }
        });
    }

    public static Props Props() => Akka.Actor.Props.Create<AllRoomsIndexActor>();
}
