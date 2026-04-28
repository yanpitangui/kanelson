using Akka.Actor;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using AwesomeAssertions;
using Kanelson.Domain.Questions;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Local;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using System.Threading.Channels;

namespace Kanelson.Tests;

public sealed class RoomSpecs : PersistenceTestKit
{
    private const string RoomId = "room-1";
    private static readonly UserInfo Owner = new("owner") { Name = "Owner" };
    private static readonly UserInfo Player = new("player") { Name = "Player" };

    [Fact]
    public async Task Local_room_subscription_should_stream_room_events()
    {
        var roomsIndexActor = Sys.ActorOf(AllRoomsIndexActor.Props(), "all-rooms-index-test");
        var roomActor = Sys.ActorOf(Room.Props(RoomId, roomsIndexActor, Sys.DeadLetters, new StubUserService(Owner, Player)), "room-actor-test");
        var roomShard = Sys.ActorOf(Props.Create(() => new FixedRoomShard(roomActor)), "room-shard-test");
        var localRoomManager = Sys.ActorOf(LocalRoomActorManager.Props(roomShard), "local-room-manager");

        var localRoom = await localRoomManager.Ask<IActorRef>(new GetLocalRoom(RoomId));

        var template = CreateTemplate();
        localRoom.Tell(new RoomCommands.SetBase(RoomId, "Room", Owner.Id, template));

        var ownerSubscription = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom(RoomId, Owner.Id, Owner.Name));
        var playerSubscription = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom(RoomId, Player.Id, Player.Name));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(ownerSubscription.Reader, evt =>
            evt.Users.Should().ContainSingle(x => x.Id == Owner.Id));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(ownerSubscription.Reader,
            evt => evt.Users.Select(x => x.Id).ToHashSet().SetEquals([Owner.Id, Player.Id]),
            evt => evt.Users.Select(x => x.Id).Should().BeEquivalentTo([Owner.Id, Player.Id]));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(playerSubscription.Reader,
            evt => evt.Users.Select(x => x.Id).ToHashSet().SetEquals([Owner.Id, Player.Id]),
            evt => evt.Users.Select(x => x.Id).Should().BeEquivalentTo([Owner.Id, Player.Id]));

        localRoom.Tell(new RoomCommands.Start(RoomId));

        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSubscription.Reader, evt =>
            evt.Status.Should().Be(RoomStatus.Started));
        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSubscription.Reader, evt =>
            evt.Status.Should().Be(RoomStatus.DisplayingQuestion));

        await ExpectEvent<RoomEvents.NextQuestion>(ownerSubscription.Reader, evt =>
            evt.Info.CurrentNumber.Should().Be(1));
        await ExpectEvent<RoomEvents.NextQuestion>(playerSubscription.Reader, evt =>
            evt.Info.CurrentNumber.Should().Be(1));

        var correctAlternative = template.Questions[0].Alternatives.Single(x => x.Correct);
        localRoom.Tell(new RoomCommands.SendUserAnswer(RoomId, Player.Id, [correctAlternative.Id]));

        await ExpectEvent<RoomEvents.UserAnswered>(ownerSubscription.Reader, evt =>
            evt.UserId.Should().Be(Player.Id));
        await ExpectEvent<RoomEvents.UserAnswered>(playerSubscription.Reader, evt =>
            evt.UserId.Should().Be(Player.Id));

        await ExpectEvent<RoomEvents.RoundFinished>(ownerSubscription.Reader, _ => { });
        await ExpectEvent<RoomEvents.RoundFinished>(playerSubscription.Reader, _ => { });

        await ExpectEvent<RoomEvents.UserRoundSummary>(playerSubscription.Reader, evt =>
            evt.Summary.Answered.Should().Contain(correctAlternative.Id));

        await ExpectEvent<RoomEvents.GameFinished>(ownerSubscription.Reader, evt =>
            evt.Rankings.Should().ContainSingle(x => x.Id == Player.Id));
        await ExpectEvent<RoomEvents.GameFinished>(playerSubscription.Reader, evt =>
            evt.Rankings.Should().ContainSingle(x => x.Id == Player.Id));

        localRoom.Tell(new UnsubscribeFromRoom(RoomId, ownerSubscription.SubscriptionId));
        localRoom.Tell(new UnsubscribeFromRoom(RoomId, playerSubscription.SubscriptionId));
    }

    [Fact]
    public async Task CurrentQuestionInfo_does_not_expose_correct_flags_to_subscribers()
    {
        var roomsIndexActor = Sys.ActorOf(AllRoomsIndexActor.Props(), "all-rooms-index-leak");
        var roomActor = Sys.ActorOf(Room.Props("room-leak", roomsIndexActor, Sys.DeadLetters, new StubUserService(Owner)), "room-actor-leak");
        var roomShard = Sys.ActorOf(Props.Create(() => new FixedRoomShard(roomActor)), "room-shard-leak");
        var localRoomManager = Sys.ActorOf(LocalRoomActorManager.Props(roomShard), "local-room-manager");

        var localRoom = await localRoomManager.Ask<IActorRef>(new GetLocalRoom("room-leak"));
        localRoom.Tell(new RoomCommands.SetBase("room-leak", "Room", Owner.Id, CreateMultiCorrectTemplate()));

        var ownerSub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-leak", Owner.Id, Owner.Name));
        localRoom.Tell(new RoomCommands.Start("room-leak"));

        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });

        var nextQ = await ExpectEvent<RoomEvents.NextQuestion>(ownerSub.Reader, _ => { });
        nextQ.Info.Question.Alternatives.Should().NotContain(a => a.Correct,
            "Correct flags must be stripped from NextQuestion broadcast");
    }

    [Fact]
    public async Task MultiCorrect_scoring_penalises_wrong_picks_and_gives_partial_credit()
    {
        var roomsIndexActor = Sys.ActorOf(AllRoomsIndexActor.Props(), "all-rooms-index-scoring");
        var player2 = new UserInfo("player2") { Name = "Player 2" };
        var roomActor = Sys.ActorOf(Room.Props("room-scoring", roomsIndexActor, Sys.DeadLetters, new StubUserService(Owner, Player, player2)), "room-actor-scoring");
        var roomShard = Sys.ActorOf(Props.Create(() => new FixedRoomShard(roomActor)), "room-shard-scoring");
        var localRoomManager = Sys.ActorOf(LocalRoomActorManager.Props(roomShard), "local-room-manager");

        var localRoom = await localRoomManager.Ask<IActorRef>(new GetLocalRoom("room-scoring"));
        var template = CreateMultiCorrectTemplate();
        localRoom.Tell(new RoomCommands.SetBase("room-scoring", "Room", Owner.Id, template));

        var ownerSub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-scoring", Owner.Id, Owner.Name));
        var playerSub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-scoring", Player.Id, Player.Name));
        var player2Sub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-scoring", player2.Id, player2.Name));

        localRoom.Tell(new RoomCommands.Start("room-scoring"));

        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.NextQuestion>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.NextQuestion>(playerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.NextQuestion>(player2Sub.Reader, _ => { });

        var correctIds = template.Questions[0].Alternatives.Where(a => a.Correct).Select(a => a.Id).ToArray();
        var wrongId = template.Questions[0].Alternatives.First(a => !a.Correct).Id;

        localRoom.Tell(new RoomCommands.SendUserAnswer("room-scoring", Player.Id, [correctIds[0]]));
        localRoom.Tell(new RoomCommands.SendUserAnswer("room-scoring", player2.Id, [correctIds[0], wrongId]));

        await ExpectEvent<RoomEvents.RoundFinished>(ownerSub.Reader, _ => { });

        var playerSummary = await ExpectEvent<RoomEvents.UserRoundSummary>(playerSub.Reader, _ => { });
        var player2Summary = await ExpectEvent<RoomEvents.UserRoundSummary>(player2Sub.Reader, _ => { });

        playerSummary.Summary.PointsEarned.Should().BeGreaterThan(0, "partial credit for 1 of 2 correct");
        playerSummary.Summary.PointsEarned.Should().BeLessThan(template.Questions[0].Points, "not full credit");
        player2Summary.Summary.PointsEarned.Should().BeLessThan(playerSummary.Summary.PointsEarned,
            "picking a wrong answer reduces score further");
    }

    private static Template CreateTemplate()
    {
        return new Template
        {
            Name = "Template",
            Questions =
            [
                new TemplateQuestion
                {
                    Name = "Question",
                    Type = QuestionType.Quiz,
                    TimeLimit = 5,
                    Points = 1000,
                    Order = 0,
                    Alternatives =
                    [
                        new Alternative { Description = "Correct", Correct = true },
                        new Alternative { Description = "Wrong", Correct = false }
                    ]
                }
            ]
        };
    }

    [Fact]
    public async Task RoundFinished_event_carries_vote_distribution()
    {
        var roomsIndexActor = Sys.ActorOf(AllRoomsIndexActor.Props(), "all-rooms-index-dist");
        var roomActor = Sys.ActorOf(Room.Props("room-dist", roomsIndexActor, Sys.DeadLetters, new StubUserService(Owner, Player)), "room-actor-dist");
        var roomShard = Sys.ActorOf(Props.Create(() => new FixedRoomShard(roomActor)), "room-shard-dist");
        var localRoomManager = Sys.ActorOf(LocalRoomActorManager.Props(roomShard), "local-room-manager");

        var localRoom = await localRoomManager.Ask<IActorRef>(new GetLocalRoom("room-dist"));
        var template = CreateTemplate();
        localRoom.Tell(new RoomCommands.SetBase("room-dist", "Room", Owner.Id, template));

        var ownerSub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-dist", Owner.Id, Owner.Name));
        var playerSub = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom("room-dist", Player.Id, Player.Name));

        localRoom.Tell(new RoomCommands.Start("room-dist"));

        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.NextQuestion>(ownerSub.Reader, _ => { });
        await ExpectEvent<RoomEvents.NextQuestion>(playerSub.Reader, _ => { });

        var correctId = template.Questions[0].Alternatives.Single(x => x.Correct).Id;
        localRoom.Tell(new RoomCommands.SendUserAnswer("room-dist", Player.Id, [correctId]));

        var roundFinished = await ExpectEvent<RoomEvents.RoundFinished>(ownerSub.Reader, _ => { });

        roundFinished.VoteDistribution.Should().HaveCount(2);
        var correctEntry = roundFinished.VoteDistribution.Single(x => x.Correct);
        correctEntry.VoteCount.Should().Be(1);
        roundFinished.VoteDistribution.Single(x => !x.Correct).VoteCount.Should().Be(0);
    }

    private static Template CreateMultiCorrectTemplate()
    {
        return new Template
        {
            Name = "MultiCorrect Template",
            Questions =
            [
                new TemplateQuestion
                {
                    Name = "Pick all correct",
                    Type = QuestionType.MultiCorrect,
                    TimeLimit = 5,
                    Points = 1000,
                    Order = 0,
                    Alternatives =
                    [
                        new Alternative { Description = "Correct A", Correct = true },
                        new Alternative { Description = "Correct B", Correct = true },
                        new Alternative { Description = "Wrong C",   Correct = false },
                        new Alternative { Description = "Wrong D",   Correct = false }
                    ]
                }
            ]
        };
    }

    private static async Task<TEvent> ExpectEvent<TEvent>(ChannelReader<IRoomEvent> reader, Action<TEvent> assert)
        where TEvent : class, IRoomEvent
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (await reader.WaitToReadAsync(cts.Token))
        {
            while (reader.TryRead(out var roomEvent))
            {
                if (roomEvent is not TEvent typed)
                {
                    continue;
                }

                assert(typed);
                return typed;
            }
        }

        throw new TimeoutException($"Timed out waiting for {typeof(TEvent).Name}.");
    }

    private static async Task<TEvent> ExpectEvent<TEvent>(ChannelReader<IRoomEvent> reader, Func<TEvent, bool> predicate, Action<TEvent> assert)
        where TEvent : class, IRoomEvent
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (await reader.WaitToReadAsync(cts.Token))
        {
            while (reader.TryRead(out var roomEvent))
            {
                if (roomEvent is not TEvent typed || !predicate(typed))
                {
                    continue;
                }

                assert(typed);
                return typed;
            }
        }

        throw new TimeoutException($"Timed out waiting for {typeof(TEvent).Name}.");
    }

    private sealed class FixedRoomShard : ReceiveActor
    {
        public FixedRoomShard(IActorRef roomActor)
        {
            ReceiveAny(message => roomActor.Forward(message));
        }
    }

    private sealed class StubUserService(params UserInfo[] users) : IUserService
    {
        private readonly Dictionary<string, UserInfo> _users = users.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        public string CurrentUser => Owner.Id;

        public void Upsert(string id, string name)
        {
            _users[id] = new UserInfo(id) { Name = name };
        }

        public Task<UserInfo> GetUserInfo(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_users[id]);
        }
    }
}
