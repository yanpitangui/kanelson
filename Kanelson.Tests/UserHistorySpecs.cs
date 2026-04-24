using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using AwesomeAssertions;
using Kanelson.Domain.Users;
using System.Collections.Immutable;

namespace Kanelson.Tests;

public sealed class UserHistorySpecs : PersistenceTestKit
{
    private const string UserId = "history-user";
    private readonly TestActorRef<UserHistory> _testActor;

    public UserHistorySpecs()
    {
        _testActor = new TestActorRef<UserHistory>(Sys, UserHistory.Props(UserId));
    }

    [Fact]
    public async Task Recording_placements_should_return_newest_first()
    {
        _testActor.Tell(new UserHistoryCommands.RecordPlacement(UserId, Placement("room-1", 2, 10, 500, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc))));
        _testActor.Tell(new UserHistoryCommands.RecordPlacement(UserId, Placement("room-2", 1, 8, 900, new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc))));

        var history = await GetHistory();

        history.Select(x => x.RoomId).Should().Equal("room-2", "room-1");
    }

    [Fact]
    public async Task Recording_more_than_100_placements_should_cap_history()
    {
        for (var i = 0; i < 120; i++)
        {
            _testActor.Tell(new UserHistoryCommands.RecordPlacement(UserId, Placement($"room-{i}", 1, 10, i, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i))));
        }

        var history = await GetHistory();

        history.Should().HaveCount(100);
        history.First().RoomId.Should().Be("room-119");
        history.Last().RoomId.Should().Be("room-20");
    }

    [Fact]
    public async Task Restarting_actor_should_recover_history()
    {
        _testActor.Tell(new UserHistoryCommands.RecordPlacement(UserId, Placement("room-1", 3, 12, 400, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc))));
        _testActor.Tell(new UserHistoryCommands.RecordPlacement(UserId, Placement("room-2", 1, 9, 1000, new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc))));

        var snapshot = await GetHistory();
        await _testActor.GracefulStop(TimeSpan.FromSeconds(3));

        var recoveringActor = new TestActorRef<UserHistory>(Sys, UserHistory.Props(UserId));
        var recovered = await recoveringActor.Ask<ImmutableArray<RoomPlacement>>(new UserHistoryQueries.GetHistory(UserId));

        recovered.Should().BeEquivalentTo(snapshot);
    }

    private Task<ImmutableArray<RoomPlacement>> GetHistory()
    {
        return _testActor.Ask<ImmutableArray<RoomPlacement>>(new UserHistoryQueries.GetHistory(UserId));
    }

    private static RoomPlacement Placement(string roomId, int rank, int totalPlayers, decimal points, DateTime playedAt)
    {
        return new RoomPlacement(roomId, $"Room {roomId}", rank, totalPlayers, points, playedAt);
    }
}
