using System.Buffers;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public class RoomIndexActor : ReceivePersistentActor, IHasSnapshotInterval
{
    public override string PersistenceId { get; }

    private RoomIndexState _state;
    private readonly Dictionary<long, IActorRef> _children;
    private readonly IHubContext<RoomHub> _hubContext;
    private readonly IUserService _userService;
    private readonly Dictionary<long, Dictionary<string, HubUser>> _roomUsers = new();


    public RoomIndexActor(string persistenceId, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        _hubContext = hubContext;
        _userService = userService;
        PersistenceId = persistenceId;

        _children = new();

        _state = new RoomIndexState();
        
        Recover<Register>(o =>
        {
            AddToChildren(o);
            HandleRegister(o);
        });
        
        Command<Register>(o =>
        {
            AddToChildren(o);
            Persist(o, HandleRegister);
        });

        Command<Exists>(o => Sender.Tell(_state.Items.Contains(o.RoomIdentifier)));

        Command<GetAllKeys>(_ => Sender.Tell(_state.Items.ToImmutableArray()));

        
        Recover<Unregister>(HandleUnregister);
        
        Command<Unregister>(o =>
        {
            Persist(o, HandleUnregister);
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

        CommandAsync<UserConnected>(async o =>
        {
            
            if(!_roomUsers.TryGetValue(o.RoomId, out var roomConnections))
            {
                roomConnections = _roomUsers[o.RoomId] = new Dictionary<string, HubUser>();
            }

            var userAdded = false;

            if(!roomConnections.TryGetValue(o.UserId, out var user))
            {
                userAdded = true;
                var userInfo = await _userService.GetUserInfo(o.UserId);
                user = roomConnections[o.UserId] = HubUser.FromUserInfo(userInfo);

            }

            user.Connections.Add(o.ConnectionId);
            var exists = _children.TryGetValue(o.RoomId, out var actorRef);
            if (Equals(actorRef, ActorRefs.Nobody) || !exists)
            {
                throw new ActorNotFoundException();
            }
            
            actorRef.Tell(o);
            
            if (!userAdded) return;

            var users = roomConnections.Values.Cast<UserInfo>().ToHashSet();
            actorRef.Tell(new UpdateCurrentUsers(users));
        });

        Command<UserDisconnected>(o =>
        {
            var rooms = _roomUsers
                .Where(x => x.Value.ContainsKey(o.UserId))
                .Select(x => new
                {
                    Room = x, 
                    User = x.Value.FirstOrDefault(y => y.Key == o.UserId).Value
                }).ToList();

            foreach (var room in rooms)
            {
                room.User.Connections.Remove(o.ConnectionId);

                var userDisconnected = false;
                if (!room.User.Connections.Any())
                {
                    userDisconnected = room.Room.Value.Remove(o.UserId);
                }
                
                if (!userDisconnected) return;
                var users = room.Room.Value.Values.Cast<UserInfo>().ToHashSet();
                var exists = _children.TryGetValue(room.Room.Key, out var actorRef);
                if (Equals(actorRef, ActorRefs.Nobody) || !exists)
                {
                    throw new ActorNotFoundException();
                }
                
                actorRef.Tell(new UpdateCurrentUsers(users));
                
            }
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
                    tasks[i] = room.Ask<RoomSummary>(new GetSummary(), TimeSpan.FromSeconds(3));
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
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomIndexState state)
            {
                _state = state;
                foreach (var item in _state.Items)
                {
                    _children[item] = GetChildRoomActorRef(item);
                }
            }
        });
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });

        Command<DeleteSnapshotsSuccess>(_ => { });
        Command<DeleteMessagesSuccess>(_ => { });

    }

    private void AddToChildren(Register o)
    {
        var roomActor = GetChildRoomActorRef(o.RoomIdentifier);
        roomActor.Tell(o.RoomBase);
        _children.Add(o.RoomIdentifier, roomActor);
    }

    private void HandleUnregister(Unregister r)
    {
        var exists = _children.TryGetValue(r.RoomIdentifier, out var child);
        if (exists || !Equals(child, ActorRefs.Nobody))
        {
            child.Tell(ShutdownCommand.Instance);
        }
        _state.Items.Remove(r.RoomIdentifier);
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
        
    }

    private void HandleRegister(Register r)
    {
        _state.Items.Add(r.RoomIdentifier);
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    private IActorRef GetChildRoomActorRef(long roomIdentifier)
    {
        return Context.ActorOf(RoomActor.Props(roomIdentifier, _hubContext, _userService), $"room-{roomIdentifier}");
    }
}


public record UserConnected(long RoomId, string UserId, string ConnectionId);

public record UserDisconnected(string UserId, string ConnectionId);

public record GetAllSummaries;

public record Register(long RoomIdentifier, SetBase RoomBase);

public record Exists(long RoomIdentifier);

public record GetRef(long RoomIdentifier);

public record GetAllKeys;

public record Unregister(long RoomIdentifier);