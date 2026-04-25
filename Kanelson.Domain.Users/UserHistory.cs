using Akka.Actor;
using Akka.Persistence;
using Kanelson.Common;
using MessagePack;
using System.Collections.Immutable;

namespace Kanelson.Domain.Users;

[MessagePackObject]
public sealed record RoomPlacement(
    [property: Key(0)] string RoomId,
    [property: Key(1)] string RoomName,
    [property: Key(2)] int Rank,
    [property: Key(3)] int TotalPlayers,
    [property: Key(4)] decimal Points,
    [property: Key(5)] DateTime PlayedAt);

public sealed class UserHistory : BaseWithSnapshotFrequencyActor
{
    private const int MaxPlacements = 100;
    private List<RoomPlacement> _state = [];

    public UserHistory(string userId)
    {
        PersistenceId = $"user-history-{userId}";

        Recover<UserHistoryCommands.RecordPlacement>(record => Apply(record.Placement));
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is List<RoomPlacement> state)
            {
                _state = state;
            }
        });

        Command<UserHistoryCommands.RecordPlacement>(record =>
        {
            Persist(record, persisted =>
            {
                Apply(persisted.Placement);
                SaveSnapshot(_state);
            });
        });

        Command<UserHistoryQueries.GetHistory>(_ =>
        {
            Sender.Tell(_state.ToImmutableArray());
        });

        Command<SaveSnapshotSuccess>(_ => { });
    }

    public override string PersistenceId { get; }

    public static Props Props(string userId) => Akka.Actor.Props.Create<UserHistory>(userId);

    private void Apply(RoomPlacement placement)
    {
        _state.Insert(0, placement);
        if (_state.Count > MaxPlacements)
        {
            _state.RemoveRange(MaxPlacements, _state.Count - MaxPlacements);
        }
    }
}
