using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Kanelson.Common;
using Kanelson.Domain.Rooms.Local;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Kanelson.Domain.Rooms;

public class Room : ReceiveActor
{
    private RoomState _state;

    private readonly IActorRef _roomsIndexActor;
    private readonly IActorRef _userHistoryShard;
    private readonly ActorSelection _localRoomManager;
    private readonly ActorMaterializer _materializer;
    private IKillSwitch? _timerKillSwitch;
    private Stopwatch? _roundStopwatch;
    private double _effectiveTimeLimit;
    private readonly string _roomIdentifier;
    private TemplateQuestion CurrentQuestion => _state.Template.Questions[_state.CurrentQuestionIdx];

    private IEnumerable<RoomUser> Users => _state.CurrentUsers.Where(x => !x.Owner);


    public Room(string roomIdentifier, IActorRef roomsIndexActor, IActorRef userHistoryShard, IUserService userService)
    {
        _roomIdentifier = roomIdentifier;
        _roomsIndexActor = roomsIndexActor;
        _userHistoryShard = userHistoryShard;
        _localRoomManager = Context.ActorSelection("/user/local-room-manager");

        _materializer = ActorMaterializer.Create(Context);

        _state = new RoomState();

        Receive<RoomCommands.SetBase>(o =>
        {
            HandleSetBase(o);
            Sender.Tell(Akka.Done.Instance);
        });

        Receive<RoomQueries.GetCurrentState>(_ =>
        {
            Sender.Tell(_state.CurrentState);
        });

        ReceiveAsync<RoomQueries.GetSummary>(async _ =>
        {
            var ownerInfo = await userService.GetUserInfo(_state.OwnerId);
            var summary = new RoomSummary(roomIdentifier,
                _state.Name,
                ownerInfo);
            Sender.Tell(summary);
        });

        Receive<RoomCommands.UpdateCurrentUsers>(HandleUpdateUsers);

        Receive<RoomQueries.GetCurrentQuestion>(_ => Sender.Tell(GetCurrentQuestionInfo()));

        Receive<RoomCommands.Start>(_ =>
        {
            SetStartedState();
            SendNextQuestion();
            SetTimeHandler();
        });

        Receive<RoundExpired>(_ => EndCurrentRound());

        Receive<RoomCommands.NextQuestion>(_ =>
        {
            if (_state.CurrentQuestionIdx + 1 > _state.MaxQuestionIdx || _state.CurrentState == RoomStatus.DisplayingQuestion) return;
            _state.CurrentQuestionIdx+= 1;
            SendNextQuestion(incrementedQuestionIdx: true);
            SetTimeHandler();
        });

        Receive<RoomCommands.ExtendTime>(o =>
        {
            if (_state.CurrentState is not RoomStatus.DisplayingQuestion) return;
            _effectiveTimeLimit += o.Seconds;
            _timerKillSwitch?.Shutdown();
            _timerKillSwitch = null;
            var elapsed = _roundStopwatch?.Elapsed.TotalSeconds ?? 0;
            var remaining = Math.Max(0, _effectiveTimeLimit - elapsed);
            _timerKillSwitch = Source
                .Single(RoundExpired.Instance)
                .Delay(TimeSpan.FromSeconds(remaining) + RoundGracePeriod)
                .ViaMaterialized(KillSwitches.Single<RoundExpired>(), Keep.Right)
                .To(Sink.ActorRef<RoundExpired>(Self, TimerStreamCompleted.Instance, _ => TimerStreamCompleted.Instance))
                .Run(_materializer);
            Broadcast(new RoomEvents.TimeExtended(o.Seconds));
        });

        Receive<RoomQueries.GetOwner>(_ => Sender.Tell(_state.OwnerId));

        Receive<RoomCommands.SendUserAnswer>(o =>
        {
            var possibleAlternatives = CurrentQuestion.Alternatives.Select(static x => x.Id);
            if (!Array.TrueForAll(o.AlternativeIds, x => possibleAlternatives.Contains(x)))
            {
                SendToUser(o.UserId, new RoomEvents.AnswerRejected(RejectionReason.InvalidAlternatives));
                return;
            }

            if (_state.CurrentState is not RoomStatus.DisplayingQuestion)
            {
                SendToUser(o.UserId, new RoomEvents.AnswerRejected(RejectionReason.InvalidState));
                return;
            }
            var questionAnswers = _state.Answers[CurrentQuestion.Id];
            var alternativeInfo = new RoomAnswer
            {
                Alternatives = o.AlternativeIds,
                TimeToAnswer = _roundStopwatch?.Elapsed ?? TimeSpan.Zero,
                Points = 0
            };
            questionAnswers.TryAdd(o.UserId, alternativeInfo);
            var user = _state.CurrentUsers.FirstOrDefault(x => string.Equals(x.Id, o.UserId, StringComparison.OrdinalIgnoreCase));
            if (user != null) user.Answered = true;
            Broadcast(new RoomEvents.UserAnswered(o.UserId));

            if (CheckEveryoneAnswered())
            {
                EndCurrentRound();
            }
        });

        Receive<RoomCommands.UserConnected>(o =>
        {
            SendToUser(o.UserId, new RoomEvents.CurrentUsersUpdated(_state.CurrentUsers));
            if(_state.CurrentState == RoomStatus.Finished)
                SendToUser(o.UserId, new RoomEvents.GameFinished(GetRanking()));
        });

        Receive<RoomCommands.Shutdown>(_ =>
        {
            _roomsIndexActor.Tell(new AllRoomsPublisherMessages.RoomUnregistered(_roomIdentifier));
            Broadcast(new RoomEvents.RoomDeleted());
            Context.Stop(Self);
        });

        Receive<ShutdownCommand>(_ =>
        {
            _roomsIndexActor.Tell(new AllRoomsPublisherMessages.RoomUnregistered(_roomIdentifier));
            Broadcast(new RoomEvents.RoomDeleted());
            Context.Stop(Self);
        });

        Receive<TimerStreamCompleted>(_ => { });
    }

    private void FillAnswersFromUsersThatHaveNotAnswered()
    {
        var usersWithAnswers = _state.Answers[CurrentQuestion.Id]
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unanswered = Users
            .Where(x => !usersWithAnswers.Contains(x.Id));

        var timeToAnswer = TimeSpan.FromSeconds(CurrentQuestion.TimeLimit);
        var question = _state.Answers[CurrentQuestion.Id];
        foreach (var user in unanswered)
        {
            question[user.Id] = new RoomAnswer
            {
                Points = 0,
                TimeToAnswer = timeToAnswer
            };
        }
    }

    private UserAnswerSummary GetUserRoundSummary(string userId)
    {
        var userAnswer = _state.Answers[CurrentQuestion.Id][userId];
        return new UserAnswerSummary(CurrentQuestion, userAnswer.Alternatives, userAnswer.Points);
    }

    private CurrentQuestionInfo GetCurrentQuestionInfo()
    {
        var question = _state.Template.Questions[_state.CurrentQuestionIdx];
        var safeQuestion = question with
        {
            Alternatives = question.Alternatives.Select(a => a with { Correct = false }).ToList()
        };
        return new CurrentQuestionInfo(safeQuestion, _state.CurrentQuestionIdx + 1, _state.MaxQuestionIdx + 1);
    }


    private ImmutableArray<UserRanking> GetRanking()
    {

        var anwsered = _state.Answers.Select(x => x.Value)
            .SelectMany(x => x)
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new
            {
                Id = x.Key,
                Points = x.Sum(y => y.Value.Points),
                Average = x.Average(y => (decimal) y.Value.TimeToAnswer.TotalSeconds)
            })
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.Average)
            .Select((x, i) => new UserRanking
            {
                Id = x.Id,
                AverageTime = x.Average,
                Points = x.Points,
                // talvez trocar isso aqui se tiver muito lerdo
                Name = _state.CurrentUsers.FirstOrDefault(y => string.Equals(y.Id, x.Id, StringComparison.OrdinalIgnoreCase))?.Name!,
                Rank = i + 1
            }).ToArray();

        return anwsered
            .ToImmutableArray();
    }
    
    private void SendNextQuestion(bool incrementedQuestionIdx = false)
    {
        _effectiveTimeLimit = CurrentQuestion.TimeLimit;
        UpdateState(RoomStatus.DisplayingQuestion);
        foreach (var user in _state.CurrentUsers)
        {
            user.Answered = false;
        }
        Broadcast(new RoomEvents.NextQuestion(GetCurrentQuestionInfo()));
    }
    
    private static readonly TimeSpan RoundGracePeriod = TimeSpan.FromSeconds(1);

    private void SetTimeHandler()
    {
        _roundStopwatch = Stopwatch.StartNew();
        _timerKillSwitch = Source
            .Single(RoundExpired.Instance)
            .Delay(TimeSpan.FromSeconds(_effectiveTimeLimit) + RoundGracePeriod)
            .ViaMaterialized(KillSwitches.Single<RoundExpired>(), Keep.Right)
            .To(Sink.ActorRef<RoundExpired>(Self, TimerStreamCompleted.Instance, _ => TimerStreamCompleted.Instance))
            .Run(_materializer);
    }



    private void UpdateState(RoomStatus destination)
    {
        _state.CurrentState = destination;
        SendToUser(_state.OwnerId, new RoomEvents.RoomStatusChanged(_state.CurrentState));
    }
     
    private decimal CalculatePoints(IEnumerable<Guid> alternativeIds, TimeSpan timeToAnswer)
    {
        var altArray = alternativeIds as Guid[] ?? alternativeIds.ToArray();

        var correctAlternatives = CurrentQuestion
            .Alternatives
            .Where(x => x.Correct)
            .Select(x => x.Id)
            .ToList();

        var maxCorrect = correctAlternatives.Count;

        var correct = correctAlternatives
            .Intersect(altArray)
            .Count();

        var wrong = CurrentQuestion.Alternatives
            .Where(x => !x.Correct)
            .Select(x => x.Id)
            .Intersect(altArray)
            .Count();

        var percentage = Math.Max(0m, (correct - wrong) / (decimal)maxCorrect);

        var timeMinusDelay = Math.Max(timeToAnswer.TotalSeconds - 1d, 0);

        // Kahoot formula: https://support.kahoot.com/hc/en-us/articles/115002303908-How-points-work
        var wouldBePoints = Math.Round((decimal)
            (1 - (timeMinusDelay / _effectiveTimeLimit) / 2) * CurrentQuestion.Points);

        return wouldBePoints * percentage;
    }

    private void EndCurrentRound()
    {
        if (_state.CurrentState is not RoomStatus.DisplayingQuestion)
        {
            return;
        }

        _timerKillSwitch?.Shutdown();
        _timerKillSwitch = null;
        _roundStopwatch?.Stop();

        var currentQuestion = CurrentQuestion;
        var answers = _state.Answers.TryGetValue(currentQuestion.Id, out var ans) ? ans : new Dictionary<string, RoomAnswer>();
        var voteDistribution = currentQuestion.Alternatives
            .Select(alt => new AlternativeVoteSummary(
                alt.Id,
                alt.Description,
                answers.Values.Count(a => a.Alternatives != null && a.Alternatives.Contains(alt.Id)),
                alt.Correct))
            .ToImmutableArray();
        Broadcast(new RoomEvents.RoundFinished(currentQuestion, voteDistribution));

        FillAnswersFromUsersThatHaveNotAnswered();

        // Score everyone now that the final effective time limit is known
        foreach (var answer in _state.Answers[currentQuestion.Id].Values)
        {
            answer.Points = CalculatePoints(answer.Alternatives, answer.TimeToAnswer);
        }

        foreach (var userId in Users.Select(x => x.Id))
        {
            SendToUser(userId, new RoomEvents.UserRoundSummary(GetUserRoundSummary(userId)));
        }

        if (_state.CurrentQuestionIdx < _state.MaxQuestionIdx)
        {
            UpdateState(RoomStatus.AwaitingForNextQuestion);
        }
        else
        {
            var rankings = GetRanking();
            RecordPlacements(rankings);
            UpdateState(RoomStatus.Finished);
            Broadcast(new RoomEvents.GameFinished(rankings));
        }
    }

    private void RecordPlacements(ImmutableArray<UserRanking> rankings)
    {
        var playedAt = DateTime.UtcNow;
        foreach (var ranking in rankings)
        {
            if (ranking.Rank is null)
            {
                continue;
            }

            _userHistoryShard.Tell(new UserHistoryCommands.RecordPlacement(
                ranking.Id,
                new RoomPlacement(
                    _roomIdentifier,
                    _state.Name,
                    ranking.Rank.Value,
                    rankings.Length,
                    ranking.Points,
                    playedAt)));
        }
    }



    private void HandleUpdateUsers(RoomCommands.UpdateCurrentUsers r)
    {
        var equal = r.Users.SetEquals(_state.CurrentUsers);

        _state.CurrentUsers = r.Users.Select(x => new RoomUser
        {
            Id = x.Id,
            Name = x.Name,
            Owner = x.Id.Equals(_state.OwnerId, StringComparison.OrdinalIgnoreCase)
        })
            .OrderByDescending(x => x.Owner)
            .ToHashSet();

        if (!equal)
        {
            Broadcast(new RoomEvents.CurrentUsersUpdated(_state.CurrentUsers));
        }
    }

    private void HandleSetBase(RoomCommands.SetBase r)
    {
        _state.OwnerId = r.OwnerId;
        _state.Template = r.Template;
        _state.Name = r.RoomName;
        _state.MaxQuestionIdx = Math.Clamp(_state.Template.Questions.Count - 1, 0, 100);
        _state.CurrentQuestionIdx = 0;
        _roomsIndexActor.Tell(new AllRoomsPublisherMessages.RoomRegistered(_roomIdentifier, r.RoomName, r.OwnerId));
    }
    
    private void SetStartedState()
    {
        UpdateState(RoomStatus.Started);
        _state.Answers.Clear();
        _state.CurrentQuestionIdx = 0;
        foreach (var question in _state.Template.Questions)
        {
            _state.Answers.TryAdd(question.Id, new Dictionary<string, RoomAnswer>(StringComparer.OrdinalIgnoreCase));
        }
    }
    
    private bool CheckEveryoneAnswered()
    {
        var users = Users.Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        // verifica se para cada usuário logado, tirando o host, existe um registro de resposta, de maneira burra
        var isEqual = users.SetEquals(_state.Answers[CurrentQuestion.Id].Keys);
        return isEqual;
    }


    private void Broadcast(IRoomEvent roomEvent)
    {
        _localRoomManager.Tell(new BroadcastEvent(_roomIdentifier, roomEvent));
    }

    private void SendToUser(string userId, IRoomEvent roomEvent)
    {
        _localRoomManager.Tell(new SendToUser(_roomIdentifier, userId, roomEvent));
    }

    public static Props Props(string roomIdentifier, IActorRef roomsIndexActor, IActorRef userHistoryShard, IUserService userService)
    {
        return Akka.Actor.Props.Create<Room>(roomIdentifier, roomsIndexActor, userHistoryShard, userService);
    }

    private sealed record RoundExpired
    {
        private RoundExpired() { }
        public static RoundExpired Instance { get; } = new();
    }

    private sealed record TimerStreamCompleted
    {
        private TimerStreamCompleted() { }
        public static TimerStreamCompleted Instance { get; } = new();
    }





}

public enum RejectionReason
{
    InvalidState,
    InvalidAlternatives
}
