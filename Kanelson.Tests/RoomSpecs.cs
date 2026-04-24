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
        var roomActor = Sys.ActorOf(Room.Props(RoomId, roomsIndexActor, new StubUserService(Owner, Player)), "room-actor-test");
        var roomShard = Sys.ActorOf(Props.Create(() => new FixedRoomShard(roomActor)), "room-shard-test");
        var localRoomManager = Sys.ActorOf(LocalRoomActorManager.Props(roomShard), "local-room-manager");

        var localRoom = await localRoomManager.Ask<IActorRef>(new GetLocalRoom(RoomId));

        localRoom.Tell(new RoomCommands.SetBase(RoomId, "Room", Owner.Id, CreateTemplate()));

        var ownerSubscription = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom(RoomId, Owner.Id, Owner.Name));
        var playerSubscription = await localRoom.Ask<SubscriptionResult>(new SubscribeToRoom(RoomId, Player.Id, Player.Name));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(ownerSubscription.Reader, evt =>
            evt.Users.Should().ContainSingle(x => x.Id == Owner.Id));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(ownerSubscription.Reader, evt =>
            evt.Users.Select(x => x.Id).Should().BeEquivalentTo([Owner.Id, Player.Id]));

        await ExpectEvent<RoomEvents.CurrentUsersUpdated>(playerSubscription.Reader, evt =>
            evt.Users.Select(x => x.Id).Should().BeEquivalentTo([Owner.Id, Player.Id]));

        localRoom.Tell(new RoomCommands.Start(RoomId));

        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSubscription.Reader, evt =>
            evt.Status.Should().Be(RoomStatus.Started));
        await ExpectEvent<RoomEvents.RoomStatusChanged>(ownerSubscription.Reader, evt =>
            evt.Status.Should().Be(RoomStatus.DisplayingQuestion));

        await ExpectEvent<RoomEvents.NextQuestion>(ownerSubscription.Reader, evt =>
            evt.Info.CurrentNumber.Should().Be(1));
        var nextQuestion = await ExpectEvent<RoomEvents.NextQuestion>(playerSubscription.Reader, evt =>
            evt.Info.CurrentNumber.Should().Be(1));

        var correctAlternative = nextQuestion.Info.Question.Alternatives.Single(x => x.Correct);
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
