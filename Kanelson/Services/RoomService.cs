using System.Buffers;
using System.Collections.Immutable;
using Orleans;
using Shared.Grains.Rooms;
using Shared.Grains.Templates;
using Shared.Models;
using shortid;

namespace Kanelson.Services;

public class RoomService : IRoomService
{
    private readonly IGrainFactory _client;
    private readonly IUserService _userService;

    public RoomService(IGrainFactory grainFactory, IUserService userService)
    {
        _client = grainFactory;
        _userService = userService;
    }
    
    public async Task<string> CreateRoom(Guid templateId, string roomName)
    {
        var manager = _client.GetGrain<ITemplateManagerGrain>(_userService.CurrentUser);
        if (!await manager.KeyExists(templateId))
        {
            throw new KeyNotFoundException();
        }

        var template = await _client.GetGrain<ITemplateGrain>(templateId).Get();
        var roomManager = _client.GetGrain<IRoomManagerGrain>(0);
        var valid = false;
        var roomId = "";
        while (!valid)
        {
            roomId = ShortId.Generate();
            valid = !await roomManager.Exists(roomId);
        }
        var room = _client.GetGrain<IRoomGrain>(roomId);
        await room.SetBase(roomName, _userService.CurrentUser, template);
        await roomManager.Register(roomId);
        return roomId;
    }

    public async Task<ImmutableArray<RoomSummary>> GetAll()
    {
        var manager = _client.GetGrain<IRoomManagerGrain>(0);
        var keys = await manager.GetAll();
        // fan out to get the individual items from the cluster in parallel
        var tasks = ArrayPool<Task<RoomSummary>>.Shared.Rent(keys.Length);
        try
        {
            // issue all individual requests at the same time
            for (var i = 0; i < keys.Length; ++i)
            {
                tasks[i] = _client.GetGrain<IRoomGrain>(keys[i]).GetSummary();
            }

            // build the result as requests complete
            var result = ImmutableArray.CreateBuilder<RoomSummary>(keys.Length);
            for (var i = 0; i < keys.Length; ++i)
            {
                var item = await tasks[i];
                
                result.Add(item);
            }
            return result.ToImmutableArray();
        }
        finally
        {
            ArrayPool<Task<RoomSummary>>.Shared.Return(tasks);
        }
    }

    public async Task<RoomSummary> Get(string id)
    {
        var manager = _client.GetGrain<IRoomManagerGrain>(0);
        if (!await manager.Exists(id))
        {
            throw new KeyNotFoundException();
        }
        var grain = _client.GetGrain<IRoomGrain>(id);
        return await grain.GetSummary();
    }

    public async Task UpdateCurrentUsers(string roomId, HashSet<UserInfo> users)
    {
        var grain = _client.GetGrain<IRoomGrain>(roomId);
        await grain.UpdateCurrentUsers(users);
    }

    public async Task<HashSet<UserInfo>> GetCurrentUsers(string roomId)
    {
        var grain = _client.GetGrain<IRoomGrain>(roomId);
        return await grain.GetCurrentUsers();
    }
}

public interface IRoomService
{
    Task<string> CreateRoom(Guid templateId, string roomName);
    Task<ImmutableArray<RoomSummary>> GetAll();
    Task<RoomSummary> Get(string id);
    Task UpdateCurrentUsers(string roomId, HashSet<UserInfo> users);

    Task<HashSet<UserInfo>> GetCurrentUsers(string roomId);
}