using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using IdGen;
using Kanelson.Actors.Rooms;
using Kanelson.Contracts.Models;

namespace Kanelson.Services;

public class RoomService : IRoomService
{
    private readonly ActorRegistry _actorRegistry;
    private readonly IIdGenerator<long> _idGenerator;
    private readonly IUserService _userService;

    public RoomService(IUserService userService, ActorRegistry actorRegistry, IIdGenerator<long> idGenerator)
    {
        _userService = userService;
        _actorRegistry = actorRegistry;
        _idGenerator = idGenerator;
    }
    
    public async Task<long> CreateRoom(Guid templateId, string roomName)
    {
        var roomId = _idGenerator.CreateId();
        // var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        // if (!await manager.KeyExists(templateId))
        // {
        //     throw new KeyNotFoundException();
        // }
        //
        // var template = await _client.GetGrain<ITemplateGrain>(templateId).Get();
        // var roomManager = _client.GetGrain<IRoomManagerGrain>(0);
        // var valid = false;
        // var roomId = "";
        // while (!valid)
        // {
        //     roomId = ShortId.Generate();
        //     valid = !await roomManager.Exists(roomId);
        // }
        // var room = _client.GetGrain<IRoomGrain>(roomId);
        // await room.SetBase(roomName, _userService.CurrentUser, template);
        // await roomManager.Register(roomId);
        // return roomId;

        return roomId;
    }

    public async Task<RoomStatus> GetCurrentState(long roomId)
    {
        var manager = _actorRegistry.Get<RoomManagerActor>();
        var room = await manager.Ask<IActorRef>(new GetRef(roomId));

        return await room.Ask<RoomStatus>(new GetCurrentState());
    }

    public async Task<ImmutableArray<RoomSummary>> GetAll()
    {
        // var manager = _client.GetGrain<IRoomManagerGrain>(0);
        // var keys = await manager.GetAll();
        // // fan out to get the individual items from the cluster in parallel
        // var tasks = ArrayPool<Task<RoomSummary>>.Shared.Rent(keys.Length);
        // try
        // {
        //     // issue all individual requests at the same time
        //     for (var i = 0; i < keys.Length; ++i)
        //     {
        //         tasks[i] = _client.GetGrain<IRoomGrain>(keys[i]).GetSummary();
        //     }
        //
        //     // build the result as requests complete
        //     var result = ImmutableArray.CreateBuilder<RoomSummary>(keys.Length);
        //     for (var i = 0; i < keys.Length; ++i)
        //     {
        //         var item = await tasks[i];
        //         
        //         result.Add(item);
        //     }
        //     return result.ToImmutableArray();
        // }
        // finally
        // {
        //     ArrayPool<Task<RoomSummary>>.Shared.Return(tasks);
        // }

        return ImmutableArray<RoomSummary>.Empty;
    }

    public async Task<RoomSummary> Get(long id)
    {
        // var manager = _client.GetGrain<IRoomManagerGrain>(0);
        // if (!await manager.Exists(id))
        // {
        //     throw new KeyNotFoundException();
        // }
        // var grain = _client.GetGrain<IRoomGrain>(id);
        // return await grain.GetSummary();

        return new RoomSummary(id, string.Empty, new UserInfo(string.Empty, string.Empty), default);
    }

    public async Task UpdateCurrentUsers(long roomId, HashSet<UserInfo> users)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // await grain.UpdateCurrentUsers(users);
    }

    public async Task<HashSet<UserInfo>> GetCurrentUsers(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // return await grain.GetCurrentUsers();
        return default;
    }

    public async Task<TemplateQuestion> GetCurrentQuestion(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // return await grain.GetCurrentQuestion();
        return default;
    }

    public async Task<bool> NextQuestion(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // return await grain.NextQuestion();
        return default;
    }

    public async Task Start(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // await grain.Start();
    }

    public async Task<string> GetOwner(long roomId)
    {
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        // return await grain.GetOwner();
        return string.Empty;
    }

    public async Task Delete(long roomId)
    {
        // var manager = _client.GetGrain<IRoomManagerGrain>(0);
        // if (!await manager.Exists(roomId))
        // {
        //     throw new KeyNotFoundException();
        // }
        // var grain = _client.GetGrain<IRoomGrain>(roomId);
        //
        // if (_userService.CurrentUser == await grain.GetOwner())
        // {
        //     await manager.Unregister(roomId);
        //     await grain.Delete();
        // }
    }

    public async Task Answer(long roomId, Guid answerId)
    {
        // var manager = _client.GetGrain<IRoomManagerGrain>(0);
        // if (!await manager.Exists(roomId))
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