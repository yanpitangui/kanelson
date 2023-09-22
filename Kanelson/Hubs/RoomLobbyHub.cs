using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Hubs;

[Authorize]
public class RoomLobbyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Rooms");

        await base.OnConnectedAsync();
    }
}