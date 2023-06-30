using Kanelson.Extensions;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Hubs;

[Authorize]
public class RoomHub : Hub
{
    private readonly IRoomService _roomService;
    
    public RoomHub(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task JoinRoom(long roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

        var userId = Context.GetUserId();
        var connectionId = Context.ConnectionId;
        _roomService.UserConnected(roomId, userId, connectionId);
    }

    public async Task Start(long roomId)
    {
        var owner = await _roomService.GetOwner(roomId);
        var userId = Context.GetUserId();
        if (string.Equals(owner, userId, StringComparison.OrdinalIgnoreCase))
        {
            await _roomService.Start(roomId);
        }
    }

    public async Task Answer(long roomId, Guid alternativeId)
    {
        await _roomService.Answer(roomId, alternativeId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetUserId();
        var connectionId = Context.ConnectionId;

        _roomService.UserDisconnected(userId, connectionId);
        await base.OnDisconnectedAsync(exception);

    }
}

public record HubUser : UserInfo
{

    public static HubUser FromUserInfo(UserInfo userInfo) => new() {Id = userInfo.Id, Name = userInfo.Name};
    public HashSet<string> Connections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}