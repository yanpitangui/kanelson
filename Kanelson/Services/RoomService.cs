using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Util;
using IdGen;
using Kanelson.Actors.Rooms;
using Kanelson.Models;
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
        var room = await GetRoomRef(roomId, index);
        return await room.Ask<RoomStatus>(Actors.Rooms.GetCurrentState.Instance);
    }

    public void UserDisconnected(string userId, string connectionId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();

        index.Tell(new UserDisconnected(userId, connectionId));
    }

    public void UserConnected(long roomId, string userId, string connectionId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        index.Tell(new UserConnected(roomId, userId, connectionId));

    }

    public async Task<ImmutableArray<RoomSummary>> GetAll()
    {
        
        var index = _actorRegistry.Get<RoomIndexActor>();

        return await index.Ask<ImmutableArray<RoomSummary>>(new GetAllSummaries());
    }

    public async Task<RoomSummary> Get(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);


        return await room.Ask<RoomSummary>(GetSummary.Instance);
    }
    
    public async Task<CurrentQuestionInfo> GetCurrentQuestion(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);


        return await room.Ask<CurrentQuestionInfo>(Actors.Rooms.GetCurrentQuestion.Instance);
    }

    public async Task NextQuestion(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);


        room.Tell(Actors.Rooms.NextQuestion.Instance);
    }

    public async Task Start(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);


        room.Tell(Actors.Rooms.Start.Instance);
    }

    public async Task<string> GetOwner(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);
        return await room.Ask<string>(Actors.Rooms.GetOwner.Instance);
    }

    public async Task Delete(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);


        var owner = await room.Ask<string>(Actors.Rooms.GetOwner.Instance, TimeSpan.FromSeconds(3));
        if (!string.Equals(owner, _userService.CurrentUser, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationException("You are not the room's owner");
        }
        index.Tell(new Unregister(roomId));
    }

    public async Task Answer(long roomId, Guid alternativeId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await GetRoomRef(roomId, index);

        room.Tell(new SendUserAnswer(_userService.CurrentUser, new []{ alternativeId }));
    }

    private static async Task<IActorRef> GetRoomRef(long roomId, IActorRef index)
    {
        var room = await index.Ask<Option<IActorRef>>(new GetRef(roomId));

        if (room.IsEmpty) throw new ActorNotFoundException();
        return room.Value;
    }
}