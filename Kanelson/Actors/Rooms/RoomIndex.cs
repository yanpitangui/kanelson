using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Persistence;
using Kanelson.Hubs;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public sealed class RoomIndex : BaseWithSnapshotFrequencyActor
{
    public override string PersistenceId { get; }

    private RoomIndexState _state;
    private readonly IActorRef _roomShard;
    private readonly IUserService _userService;
    private readonly Dictionary<string, Dictionary<string, HubUser>> _roomUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IActorRef _signalrActor;


    public RoomIndex(string persistenceId, IActorRef roomShard,
        IHubContext<RoomLobbyHub> roomLobbyContext, IUserService userService)
    {
        _roomShard = roomShard;
        _userService = userService;
        _signalrActor = Context.ActorOf(SignalrActor.Props((IHubContext) roomLobbyContext));

        PersistenceId = persistenceId;


        _state = new RoomIndexState();
        
        Recover<Register>(HandleRegister);
        
        Command<Register>(o =>
        {
            Persist(o, HandleRegister);
        });
        
        Recover<Unregister>(HandleUnregister);
        
        Command<Unregister>(o =>
        {
            Persist(o, HandleUnregister);
        });

        Command<Exists>(o =>
        {
            Sender.Tell(_state.Items.Keys.Contains(o.RoomId, StringComparer.OrdinalIgnoreCase));
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

            
            roomShard.Tell(MessageEnvelope(o.RoomId, o));
            
            if (!userAdded) return;

            var users = roomConnections.Values.Cast<UserInfo>().ToHashSet();
            roomShard.Tell(MessageEnvelope(o.RoomId, new UpdateCurrentUsers(users)));
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
                roomShard.Tell(MessageEnvelope(room.Room.Key, new UpdateCurrentUsers(users)));
                
            }
        });

        Command<GetRoomsBasicInfo>(_ =>
        {
            Sender.Tell(_state.Items.Values.ToImmutableArray());
        });
        
        Command<SaveSnapshotSuccess>(_ => { });

        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomIndexState state)
            {
                _state = state;
            }
        });

    }

    private void HandleUnregister(Unregister r)
    {
        _state.Items.Remove(r.RoomId);
        GenerateChangedSignalRMessage().PipeTo(_signalrActor);
        SaveSnapshotIfPassedInterval(_state);
        
    }

    private async Task<SendSignalrGroupMessage> GenerateChangedSignalRMessage()
    {
        var summary = await Self.Ask<ImmutableArray<BasicRoomInfo>>(GetRoomsBasicInfo.Instance);
        return new SendSignalrGroupMessage(RoomLobbyHub.RoomsGroup, RoomLobbyHub.SignalRMessages.RoomsChanged, summary);
    }

    private void HandleRegister(Register r)
    {
        _roomShard.Tell(MessageEnvelope(r.RoomId, r.RoomBase));
        _state.Items.Add(r.RoomId, new BasicRoomInfo(r.RoomId, r.RoomBase.RoomName, r.RoomBase.OwnerId));
        GenerateChangedSignalRMessage().PipeTo(_signalrActor);
        SaveSnapshotIfPassedInterval(_state);
    }

    private static ShardingEnvelope MessageEnvelope<T>(string id, T message) where T: class
    {
        return new ShardingEnvelope(id, message);
    }


    public static Props Props(string persistenceId, IActorRef roomShard,
        IHubContext<RoomLobbyHub> roomLobbyContext, IUserService userService)
    {
        return Akka.Actor.Props.Create<RoomIndex>(persistenceId, roomShard, roomLobbyContext, userService);

    }
}


public record UserConnected(string RoomId, string UserId, string ConnectionId);

public record UserDisconnected(string UserId, string ConnectionId);

public record GetRoomsBasicInfo
{
    private GetRoomsBasicInfo()
    {
    }

    public static GetRoomsBasicInfo Instance { get; } = new();
}

public record Register(string RoomId, SetBase RoomBase);

public record Exists(string RoomId);

public record Unregister(string RoomId);