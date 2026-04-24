using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates.Models;
using System.Collections.Immutable;

namespace Kanelson.Domain.Rooms;

public interface IRoomEvent { }

public static class RoomEvents
{
    public record CurrentUsersUpdated(HashSet<RoomUser> Users) : IRoomEvent;
    public record RoomStatusChanged(RoomStatus Status) : IRoomEvent;
    public record NextQuestion(CurrentQuestionInfo Info) : IRoomEvent;
    public record RoundFinished(TemplateQuestion Question) : IRoomEvent;
    public record UserRoundSummary(UserAnswerSummary Summary) : IRoomEvent;
    public record UserAnswered(string UserId) : IRoomEvent;
    public record GameFinished(ImmutableArray<UserRanking> Rankings) : IRoomEvent;
    public record RoomDeleted : IRoomEvent;
    public record AnswerRejected(RejectionReason Reason) : IRoomEvent;
}
