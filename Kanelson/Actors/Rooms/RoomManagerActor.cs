using System.Collections.Immutable;
using Akka.Actor;

namespace Kanelson.Actors.Rooms;

public class RoomManagerActor : ReceiveActor
{
    //public override string PersistenceId { get; }

    private readonly RoomManagerState _state;
    private readonly Dictionary<long, IActorRef> _children;

    public RoomManagerActor()
    {
        //PersistenceId = "room-manager";

        _children = new();

        _state = new RoomManagerState();
        Receive<Register>(o =>
        {
            _state.Items.Add(o.RoomIdentifier);
            _children.Add(o.RoomIdentifier, Context.ActorOf(RoomActor.Props(o.RoomIdentifier)));
        });

        Receive<Exists>(o => Sender.Tell(_state.Items.Contains(o.RoomIdentifier)));

        Receive<GetAll>(_ => Sender.Tell(_state.Items.ToImmutableArray()));

        Receive<Unregister>(o =>
        {
            _state.Items.Remove(o.RoomIdentifier);
            var exists = _children.TryGetValue(o.RoomIdentifier, out var child);
            if (exists || !Equals(child, ActorRefs.Nobody))
            {
                child.Tell(PoisonPill.Instance);
            }
        });

        Receive<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.RoomIdentifier, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                throw new ActorNotFoundException();
            }
            Sender.Tell(actorRef);
            
        });

    }
}


public record Register(long RoomIdentifier);

public record Exists(long RoomIdentifier);

public record GetRef(long RoomIdentifier);

public record GetAll;

public record Unregister(long RoomIdentifier);