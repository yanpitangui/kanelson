using System.Collections.Immutable;
using Akka.Actor;

namespace Kanelson.Actors.Rooms;

public class RoomManagerActor : ReceiveActor
{
    //public override string PersistenceId { get; }

    private readonly RoomManagerState _state;
    public RoomManagerActor()
    {
        //PersistenceId = "room-manager";

        _state = new RoomManagerState();
        Receive<Register>(o => _state.Items.Add(o.RoomIdentifier));

        Receive<Exists>(o => Sender.Tell(_state.Items.Contains(o.RoomIdentifier)));

        Receive<GetAll>(_ => Sender.Tell(_state.Items.ToImmutableArray()));

        Receive<Unregister>(o => _state.Items.Remove(o.RoomIdentifier));

    }
}


public record Register(string RoomIdentifier);

public record Exists(string RoomIdentifier);

public record GetAll;

public record Unregister(string RoomIdentifier);