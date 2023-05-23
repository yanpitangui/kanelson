using System.Buffers;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public class RoomIndexActor : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    private readonly RoomIndexState _state;
    private readonly Dictionary<long, IActorRef> _children;
    private readonly IHubContext<RoomHub> _hubContext;
    private readonly IUserService _userService;

    public RoomIndexActor(string persistenceId, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        _hubContext = hubContext;
        _userService = userService;
        PersistenceId = persistenceId;

        _children = new();

        _state = new RoomIndexState();
        Command<Register>(o =>
        {
            _state.Items.Add(o.RoomIdentifier);

            var roomActor = Context.ActorOf(RoomActor.Props(o.RoomIdentifier, _hubContext, _userService), $"room-{o.RoomIdentifier}");
            
            roomActor.Tell(o.RoomBase);
            _children.Add(o.RoomIdentifier, roomActor);
        });

        Command<Exists>(o => Sender.Tell(_state.Items.Contains(o.RoomIdentifier)));

        Command<GetAllKeys>(_ => Sender.Tell(_state.Items.ToImmutableArray()));

        Command<Unregister>(o =>
        {
            _state.Items.Remove(o.RoomIdentifier);
            var exists = _children.TryGetValue(o.RoomIdentifier, out var child);
            if (exists || !Equals(child, ActorRefs.Nobody))
            {
                child.Tell(PoisonPill.Instance);
            }
        });

        Command<GetRef>(o =>
        {
            var exists = _children.TryGetValue(o.RoomIdentifier, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                throw new ActorNotFoundException();
            }
            Sender.Tell(actorRef);
            
        });

        CommandAsync<GetAllSummaries>(async _ =>
        {

            var keys = _state.Items.ToArray();
            var tasks = ArrayPool<Task<RoomSummary>>.Shared.Rent(keys.Length);
            try
            {
                // issue all individual requests at the same time
                for (var i = 0; i < keys.Length; ++i)
                {
                    var room = _children[keys[i]];
                    tasks[i] = room.Ask<RoomSummary>(new GetSummary());
                }
        
                // build the result as requests complete
                var result = ImmutableArray.CreateBuilder<RoomSummary>(keys.Length);
                for (var i = 0; i < keys.Length; ++i)
                {
                    var item = await tasks[i];
                
                    result.Add(item);
                }
                Sender.Tell(result.ToImmutableArray());
            }
            finally
            {
                ArrayPool<Task<RoomSummary>>.Shared.Return(tasks);
            }
        });

    }
}


public record GetAllSummaries;

public record Register(long RoomIdentifier, SetBase RoomBase);

public record Exists(long RoomIdentifier);

public record GetRef(long RoomIdentifier);

public record GetAllKeys;

public record Unregister(long RoomIdentifier);