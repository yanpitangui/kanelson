using System.Collections.Immutable;
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

    public async Task<RoomSummary> Get(long id)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(id));

        return await room.Ask<RoomSummary>(new GetSummary());
    }

    public async Task<HashSet<RoomUser>> GetCurrentUsers(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));

        return await room.Ask<HashSet<RoomUser>>(new GetCurrentUsers());
    }

    public async Task<CurrentQuestionInfo> GetCurrentQuestion(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        return await room.Ask<CurrentQuestionInfo>(new GetCurrentQuestion());
    }

    public async Task NextQuestion(long roomId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId));
        room.Tell(new NextQuestion());
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
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId), TimeSpan.FromSeconds(3));
        var owner = await room.Ask<string>(new GetOwner(), TimeSpan.FromSeconds(3));
        if (owner != _userService.CurrentUser)
        {
            throw new ApplicationException("You are not the room's owner");
        }
        index.Tell(new Unregister(roomId));
    }

    public async Task Answer(long roomId, Guid answerId)
    {
        var index = _actorRegistry.Get<RoomIndexActor>();
        var room = await index.Ask<IActorRef>(new GetRef(roomId), TimeSpan.FromSeconds(3));
        room.Tell(new SendUserAnswer(_userService.CurrentUser, answerId));
    }
}

public interface IRoomService
{
    Task<long> CreateRoom(Guid templateId, string roomName);
    Task<ImmutableArray<RoomSummary>> GetAll();
    Task<RoomSummary> Get(long id);
    Task<HashSet<RoomUser>> GetCurrentUsers(long roomId);
    Task<CurrentQuestionInfo> GetCurrentQuestion(long roomId);
    Task NextQuestion(long roomId);
    Task Start(long roomId);
    Task<string> GetOwner(long roomId);
    Task Delete(long roomId);
    Task Answer(long roomId, Guid answerId);
    Task<RoomStatus> GetCurrentState(long roomId);
    void UserDisconnected(string userId, string connectionId);
    void UserConnected(long roomId, string userId, string connectionId);
}