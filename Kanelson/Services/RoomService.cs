﻿using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using IdGen;
using Kanelson.Actors.Rooms;
using Kanelson.Models;
using System.Globalization;
using Register = Kanelson.Actors.Rooms.Register;

namespace Kanelson.Services;

public class RoomService : IRoomService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IIdGenerator<long> _idGenerator;
    private readonly ITemplateService _templateService;
    private readonly IUserService _userService;

    public RoomService(IUserService userService,
        ActorRegistry actorRegistry, 
        IIdGenerator<long> idGenerator,
        ITemplateService templateService)
    {
        _userService = userService;
        _actorRegistry = actorRegistry;
        _idGenerator = idGenerator;
        _templateService = templateService;
    }
    
    public async Task<string> CreateRoom(Guid templateId, string roomName, CancellationToken ct = default)
    {
        var roomId = _idGenerator.CreateId().ToString(NumberFormatInfo.InvariantInfo);
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var template = await _templateService.GetTemplate(templateId);

        index.Tell(new Register(new SetBase(roomId, roomName, _userService.CurrentUser, template)));

        return roomId;
    }

    public async Task<RoomStatus> GetCurrentState(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);
        return await roomShardingRef.Ask<RoomStatus>(new GetCurrentState(roomId), ct);
    }

    public void UserDisconnected(string userId, string connectionId)
    {
        var index = _actorRegistry.Get<RoomIndex>();

        index.Tell(new UserDisconnected(userId, connectionId));
    }

    public void UserConnected(string roomId, string userId, string connectionId)
    {
        var index = _actorRegistry.Get<RoomIndex>();
        index.Tell(new UserConnected(roomId, userId, connectionId));

    }

    public async Task<ImmutableArray<BasicRoomInfo>> GetAll(CancellationToken ct = default)
    {
        
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);

        return await index.Ask<ImmutableArray<BasicRoomInfo>>(GetRoomsBasicInfo.Instance, ct);
    }

    public async Task<RoomSummary> Get(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);


        return await roomShardingRef.Ask<RoomSummary>(new GetSummary(roomId), ct);
    }
    
    public async Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);


        return await roomShardingRef.Ask<CurrentQuestionInfo>(new GetCurrentQuestion(roomId), ct);
    }

    public async Task NextQuestion(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);


        roomShardingRef.Tell(new NextQuestion(roomId));
    }

    public async Task Start(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);


        roomShardingRef.Tell(new Start(roomId));
    }
    

    public async Task Delete(string roomId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);


        var owner = await roomShardingRef.Ask<string>(new GetOwner(roomId), ct);
        if (!string.Equals(owner, _userService.CurrentUser, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationException("You are not the room's owner");
        }
        index.Tell(new Unregister(roomId));
    }

    public async Task Answer(string roomId, Guid alternativeId, CancellationToken ct = default)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>(ct);
        var roomShardingRef = await GetRoomShardingRef(roomId, index, ct);

        roomShardingRef.Tell(new SendUserAnswer(roomId, _userService.CurrentUser, new []{ alternativeId }));
    }

    private async Task<IActorRef> GetRoomShardingRef(string roomId, IActorRef index, CancellationToken ct = default)
    {
        var exists = await index.Ask<bool>(new Exists(roomId), ct);

        if (!exists) throw new ActorNotFoundException();
        return await _actorRegistry.GetAsync<Room>(ct);
    }
}