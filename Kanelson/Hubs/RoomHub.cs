using Kanelson.Extensions;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;

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
    
    public static class SignalRMessages
    {
        public const string CurrentUsersUpdated = nameof(CurrentUsersUpdated);
        public const string RoomStatusChanged = nameof(RoomStatusChanged);
        public const string NextQuestion = nameof(NextQuestion);
        public const string RoundFinished = nameof(RoundFinished);
        public const string Answer = nameof(Answer);
        public const string JoinRoom = nameof(JoinRoom);
        public const string UserAnswered = nameof(UserAnswered);
        public const string RoomDeleted = nameof(RoomDeleted);
        public const string RoomFinished = nameof(RoomFinished);
        public const string RoundSummary = nameof(RoundSummary);
        public const string AnswerRejected = nameof(AnswerRejected);
    }
}

public record HubUser : UserInfo
{

    public static HubUser FromUserInfo(UserInfo userInfo) => new() {Id = userInfo.Id, Name = userInfo.Name};
    public HashSet<string> Connections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}