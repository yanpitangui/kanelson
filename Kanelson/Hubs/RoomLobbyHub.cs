using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Hubs;

[Authorize]
public class RoomLobbyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomsGroup);

        await base.OnConnectedAsync();
    }


    public static class SignalRMessages
    {
        public const string RoomsChanged = nameof(RoomsChanged);
    }

    public const string RoomsGroup = "Rooms";
}