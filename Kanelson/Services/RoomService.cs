﻿using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using IdGen;
using Kanelson.Actors.Rooms;
using Kanelson.Contracts.Models;
using GetRef = Kanelson.Actors.Rooms.GetRef;
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
    
    public async Task<long> CreateRoom(Guid templateId, string roomName)
    {
        var roomId = _idGenerator.CreateId();
        var index = _actorRegistry.Get<RoomIndexActor>();
        var template = await _templateService.GetTemplate(templateId);

        index.Tell(new Register(roomId, new SetBase(roomName, _userService.CurrentUser, template)));

        return roomId;
    }

    public async Task<RoomStatus> GetCurrentState(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));

        return await room.Ask<RoomStatus>(new GetCurrentState());
    }

    public async Task<ImmutableArray<RoomSummary>> GetAll()
    {
        
        var index = _actorRegistry.Get<RoomIndexActor>();

        return await index.Ask<ImmutableArray<RoomSummary>>(new GetAllSummaries());
    }

    public async Task<RoomSummary> Get(long id)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(id));

        return await room.Ask<RoomSummary>(new GetSummary());
    }

    public async Task UpdateCurrentUsers(long roomId, HashSet<UserInfo> users)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        room.Tell(new UpdateCurrentUsers(users));
    }

    public async Task<HashSet<UserInfo>> GetCurrentUsers(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));

        return await room.Ask<HashSet<UserInfo>>(new GetCurrentUsers());
    }

    public async Task<TemplateQuestion> GetCurrentQuestion(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        return await room.Ask<TemplateQuestion>(new GetCurrentQuestion());
    }

    public async Task<bool> NextQuestion(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // return await grain.NextQuestion();
        return default;
    }

    public async Task Start(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        room.Tell(new Start());
    }

    public async Task<string> GetOwner(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        return await room.Ask<string>(new GetOwner());
    }

    public async Task Delete(long roomId)
    {
        // var index = _client.GetGrain<IRoomManagerGrain>(0);
        // if (!await index.Exists(roomId))
        // {
        //     throw new KeyNotFoundException();
        // }
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        //
        // if (_userService.CurrentUser == await grain.GetOwner())
        // {
        //     await index.Unregister(roomId);
        //     await grain.Delete();
        // }
    }

    public async Task Answer(long roomId, Guid answerId)
    {
        // var index = _client.GetGrain<IRoomManagerGrain>(0);
        // if (!await index.Exists(roomId))
        // {
        //     throw new KeyNotFoundException();
        // }
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // await grain.Answer(userId, roomId, answerId);
    }
}

public interface IRoomService
{
    Task<long> CreateRoom(Guid templateId, string roomName);
    Task<ImmutableArray<RoomSummary>> GetAll();
    Task<RoomSummary> Get(long id);
    Task UpdateCurrentUsers(long roomId, HashSet<UserInfo> users);

    Task<HashSet<UserInfo>> GetCurrentUsers(long roomId);
    Task<TemplateQuestion> GetCurrentQuestion(long roomId);
    Task<bool> NextQuestion(long roomId);
    Task Start(long roomId);
    Task<string> GetOwner(long roomId);
    Task Delete(long roomId);
    Task Answer(long roomId, Guid answerId);
    Task<RoomStatus> GetCurrentState(long roomId);
}