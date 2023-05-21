using System.Collections.Concurrent;
using Kanelson.Extensions;
using Kanelson.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Kanelson.Contracts.Models;

namespace Kanelson.Hubs;

[Authorize]
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
        var roomOwner = await _roomService.GetOwner(roomId);
        if (userId == roomOwner) return;
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
    }

    public async Task Start(string roomId)
    {
        var owner = await _roomService.GetOwner(roomId);
        var userId = Context.GetUserId();
        if (owner == userId)
        {
            await _roomService.Start(roomId);
        }
    }

    public async Task Answer(string roomId, Guid answerId)
    {
        var userId = Context.GetUserId();
        await _roomService.Answer(userId, roomId, answerId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetUserId();
        var connectionId = Context.ConnectionId;

        var rooms = RoomUsers
            .Where(x => x.Value.ContainsKey(userId))
            .Select(x => new
            {
                Room = x, 
                User = x.Value.FirstOrDefault(y => y.Key == userId).Value
            }).ToList();

        foreach (var room in rooms)
        {

            lock (room.User.Connections)
            {
                room.User.Connections.Remove(connectionId);
            }

            if (!room.User.Connections.Any())
            {
                lock (room.Room.Value)
                {
                    room.Room.Value.TryRemove(userId, out _);
                }
            }
            
            var users = room.Room.Value.Values.Cast<UserInfo>().ToHashSet();
            await _roomService.UpdateCurrentUsers(room.Room.Key, users);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

public record HubUser : UserInfo
{
    public HashSet<string> Connections { get; set; } = new();
}