using System.Collections.Concurrent;
using Kanelson.Extensions;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
using Shared.Models;

namespace Kanelson.Hubs;

public class RoomHub : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HubUser>> RoomUsers = new();
    private readonly IRoomService _roomService;
    private readonly IUserService _userService;
    
    public RoomHub(IRoomService roomService, IUserService userService)
    {
        _roomService = roomService;
        _userService = userService;
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var userId = Context.GetUserId();
        var connectionId = Context.ConnectionId;
        var userInfo = await _userService.GetUserInfo(userId);

        var room = RoomUsers.GetOrAdd(roomId, _ => 
            new ConcurrentDictionary<string, HubUser>());
        
        var user = room.GetOrAdd(userId, _ => new HubUser()
        {
            Id = userInfo.Id,
            Name = userInfo.Name
        });

        lock (user.Connections)
        {
            user.Connections.Add(connectionId);
        }

        var users = room.Values.Cast<UserInfo>().ToHashSet();
        await _roomService.UpdateCurrentUsers(roomId, users);

        await Clients.Group(roomId).SendAsync("CurrentUsersUpdated", users);

    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetUserId();
        
        return base.OnDisconnectedAsync(exception);
    }
}

public record HubUser : UserInfo
{
    public HashSet<string> Connections { get; set; } = new();
}