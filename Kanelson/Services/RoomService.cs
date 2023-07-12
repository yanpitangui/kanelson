﻿using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Util;
using IdGen;
using Kanelson.Actors.Rooms;
using Kanelson.Models;
using System.Globalization;
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
    
    public async Task<string> CreateRoom(Guid templateId, string roomName)
    {
        var roomId = _idGenerator.CreateId().ToString(NumberFormatInfo.InvariantInfo);
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var template = await _templateService.GetTemplate(templateId);

        index.Tell(new Register(roomId, new SetBase(roomName, _userService.CurrentUser, template)));

        return roomId;
    }

    public async Task<RoomStatus> GetCurrentState(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);
        return await room.Ask<RoomStatus>(Actors.Rooms.GetCurrentState.Instance, TimeSpan.FromSeconds(3));
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

    public async Task<ImmutableArray<RoomSummary>> GetAll()
    {
        
        var index = await _actorRegistry.GetAsync<RoomIndex>();

        return await index.Ask<ImmutableArray<RoomSummary>>(GetAllSummaries.Instance, TimeSpan.FromSeconds(3));
    }

    public async Task<RoomSummary> Get(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);


        return await room.Ask<RoomSummary>(GetSummary.Instance, TimeSpan.FromSeconds(3));
    }
    
    public async Task<CurrentQuestionInfo> GetCurrentQuestion(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);


        return await room.Ask<CurrentQuestionInfo>(Actors.Rooms.GetCurrentQuestion.Instance, TimeSpan.FromSeconds(3));
    }

    public async Task NextQuestion(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);


        room.Tell(Actors.Rooms.NextQuestion.Instance);
    }

    public async Task Start(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);


        room.Tell(Actors.Rooms.Start.Instance);
    }
    

    public async Task Delete(string roomId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);


        var owner = await room.Ask<string>(Actors.Rooms.GetOwner.Instance, TimeSpan.FromSeconds(3));
        if (!string.Equals(owner, _userService.CurrentUser, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationException("You are not the room's owner");
        }
        index.Tell(new Unregister(roomId));
    }

    public async Task Answer(string roomId, Guid alternativeId)
    {
        var index = await _actorRegistry.GetAsync<RoomIndex>();
        var room = await GetRoomRef(roomId, index);

        room.Tell(new SendUserAnswer(_userService.CurrentUser, new []{ alternativeId }));
    }

    private static async Task<IActorRef> GetRoomRef(string roomId, IActorRef index)
    {
        var room = await index.Ask<Option<IActorRef>>(new GetRef(roomId), TimeSpan.FromSeconds(3));

        if (room.IsEmpty) throw new ActorNotFoundException();
        return room.Value;
    }
}