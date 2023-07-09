using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Akka.Util;
using Kanelson.Hubs;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public sealed class RoomIndex : BaseWithSnapshotFrequencyActor
{
    public override string PersistenceId { get; }

    private RoomIndexState _state;
    private readonly Dictionary<long, IActorRef> _children;
    private readonly IHubContext<RoomHub> _roomContext;
    private readonly IUserService _userService;
    private readonly Dictionary<long, Dictionary<string, HubUser>> _roomUsers = new();
    private readonly IActorRef _signalrActor;


    public RoomIndex(string persistenceId, IHubContext<RoomHub> roomContext,
        IHubContext<RoomLobbyHub> roomLobbyContext, IUserService userService)
    {
        _roomContext = roomContext;
        _userService = userService;
        _signalrActor = Context.ActorOf(SignalrActor.Props((IHubContext) roomLobbyContext));

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
                Sender.Tell(Option<IActorRef>.Create(null!));
            }
            Sender.Tell(Option<IActorRef>.Create(actorRef!));
            
        });

        CommandAsync<UserConnected>(async o =>
        {
            
            if(!_roomUsers.TryGetValue(o.RoomId, out var roomConnections))
            {
                roomConnections = _roomUsers[o.RoomId] = new Dictionary<string, HubUser>(StringComparer.OrdinalIgnoreCase);
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
                    User = x.Value.FirstOrDefault(y => string.Equals(y.Key, o.UserId, StringComparison.OrdinalIgnoreCase)).Value
                }).ToList();

            foreach (var room in rooms)
            {
                room.User.Connections.Remove(o.ConnectionId);

                var userDisconnected = false;
                if (!room.User.Connections.Any())
                {
                    userDisconnected = room.Room.Value.Remove(o.UserId);
                }
                
                if (!userDisconnected) continue;
                var users = room.Room.Value.Values.Cast<UserInfo>().ToHashSet();
                var exists = _children.TryGetValue(room.Room.Key, out var actorRef);
                if (Equals(actorRef, ActorRefs.Nobody) || !exists)
                {
                    continue;
                }
                
                actorRef.Tell(new UpdateCurrentUsers(users));
                
            }
        });

        Command<GetAllSummaries>(_ =>
        {
            var sender = Sender;
            
            var tasks = _state.Items.Select(x => _children[x].Ask<RoomSummary>(GetSummary.Instance, TimeSpan.FromSeconds(3)));
            
            async Task<ImmutableArray<RoomSummary>> ExecuteWork()
            {
                var result = await Task.WhenAll(tasks);
                return result.ToImmutableArray();
            }


            ExecuteWork().PipeTo(sender);
        });
        
        Command<SaveSnapshotSuccess>(_ => { });

        
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

    }

    private void AddToChildren(Register o)
    {
        var roomActor = GetChildRoomActorRef(o.RoomIdentifier);
        roomActor.Tell(o.RoomBase);
        _children.TryAdd(o.RoomIdentifier, roomActor);
    }

    private void HandleUnregister(Unregister r)
    {
        var exists = _children.TryGetValue(r.RoomIdentifier, out var child);
        if (exists || !Equals(child, ActorRefs.Nobody))
        {
            child.Tell(ShutdownCommand.Instance);
        }
        _state.Items.Remove(r.RoomIdentifier);
        _children.Remove(r.RoomIdentifier);
        GenerateChangedSignalRMessage().PipeTo(_signalrActor);
        SaveSnapshotIfPassedInterval(_state);
        
    }

    private async Task<SendSignalrGroupMessage> GenerateChangedSignalRMessage()
    {
        var summary = await Self.Ask<ImmutableArray<RoomSummary>>(GetAllSummaries.Instance);
        return new SendSignalrGroupMessage(RoomLobbyHub.RoomsGroup, RoomLobbyHub.SignalRMessages.RoomsChanged, summary);
    }

    private void HandleRegister(Register r)
    {
        _state.Items.Add(r.RoomIdentifier);
        GenerateChangedSignalRMessage().PipeTo(_signalrActor);
        SaveSnapshotIfPassedInterval(_state);
    }

    private IActorRef GetChildRoomActorRef(long roomIdentifier)
    {
        return Context.ActorOf(Room.Props(roomIdentifier, _roomContext, _userService), $"room-{roomIdentifier}");
    }
}


public record UserConnected(long RoomId, string UserId, string ConnectionId);

public record UserDisconnected(string UserId, string ConnectionId);

public record GetAllSummaries
{
    private GetAllSummaries()
    {
    }

    public static GetAllSummaries Instance { get; } = new();
}

public record Register(long RoomIdentifier, SetBase RoomBase);

public record GetRef(long RoomIdentifier);


public record Unregister(long RoomIdentifier);