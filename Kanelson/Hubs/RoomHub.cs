using Kanelson.Domain.Rooms;
using Kanelson.Domain.Users;
using Kanelson.Extensions;
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

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var userId = Context.GetUserId();
        var connectionId = Context.ConnectionId;
        _roomService.UserConnected(roomId, userId, connectionId);
    }

    public async Task Answer(string roomId, Guid alternativeId)
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

