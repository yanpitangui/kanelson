namespace Kanelson.Actors.Rooms;

public class RoomQueries
{
    public record Exists(string RoomId) : IWithRoomId;

    public sealed record GetRoomsBasicInfo
    {
        private GetRoomsBasicInfo()
        {
        }

        public static GetRoomsBasicInfo Instance { get; } = new();
    }
    
    public sealed record GetCurrentState(string RoomId) : IWithRoomId;

    public sealed record GetSummary(string RoomId) : IWithRoomId;
    
    public sealed record GetCurrentQuestion(string RoomId) : IWithRoomId;

    public sealed record GetOwner(string RoomId) : IWithRoomId;
    
}