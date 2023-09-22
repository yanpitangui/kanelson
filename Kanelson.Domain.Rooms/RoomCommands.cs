using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;

namespace Kanelson.Domain.Rooms;

public static class RoomCommands
{
    public sealed record Register(SetBase RoomBase);
    
    public sealed record Unregister(string RoomId) : IWithRoomId;
    
    public sealed record UserConnected(string RoomId, string UserId, string ConnectionId) : IWithRoomId;

    public sealed record UserDisconnected(string UserId, string ConnectionId);
    
    public sealed record SetBase(string RoomId, string RoomName, string OwnerId, Template Template) : IWithRoomId;

  


    public sealed record UpdateCurrentUsers(string RoomId, HashSet<UserInfo> Users) : IWithRoomId;



    public sealed record Start(string RoomId) : IWithRoomId;


    public sealed record NextQuestion(string RoomId) : IWithRoomId;
    
    public sealed record SendUserAnswer(string RoomId, string UserId, Guid[] AlternativeIds) : IWithRoomId;

    
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
    public const string RoomsChanged = nameof(RoomsChanged);
    public const string AnswerRejected = nameof(AnswerRejected);
}