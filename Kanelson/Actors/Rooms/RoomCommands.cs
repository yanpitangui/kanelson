using Kanelson.Models;

namespace Kanelson.Actors.Rooms;

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