using System.Collections.Immutable;
using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public class RoomActor : ReceivePersistentActor, IHasSnapshotInterval, IWithTimers
{
    private const string AnswerloopTimerName = "AnswerLoop";
    private readonly long _roomIdentifier;

    public override string PersistenceId { get; }

    private RoomState _state;
    private RoomStateMachine _roomStateMachine = null!;
    
    
    private DateTime _currentQuestionStartTime;
    private readonly string _roomIdentifierString;
    private TemplateQuestion CurrentQuestion => _state.Template.Questions[_state.CurrentQuestionIdx];



    public RoomActor(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        _roomIdentifier = roomIdentifier;
        _roomIdentifierString = _roomIdentifier.ToString();
        PersistenceId = $"room-{roomIdentifier}";
        
        _state = new RoomState();
        SetupStateTriggers();
        
        Recover<SetBase>(HandleSetBase);
        
        Command<SetBase>(o =>
        {
            Persist(o, HandleSetBase);
        });
        
        Command<GetCurrentState>(_ =>
        {
            Sender.Tell(_state.CurrentState);
        });
        
        CommandAsync<GetSummary>(async _ =>
        {
            var ownerInfo = await userService.GetUserInfo(_state.OwnerId); 
            var summary = new RoomSummary(roomIdentifier,
                _state.Name,
                ownerInfo);
            Sender.Tell(summary);
        });

        Recover<UpdateCurrentUsers>(HandleUpdateUsers);

        Command<SendSignalrGroupMessage>(o =>
        {
            hubContext.Clients.Group(o.GroupId).SendAsync(o.MessageName, o.Data).PipeTo(Sender, Self);
        });

        Command<SendSignalrUserMessage>(o =>
        {
            hubContext.Clients.User(o.UserId).SendAsync(o.MessageName, o.Data).PipeTo(Sender, Self);
        });


        Command<UpdateCurrentUsers>(o =>
        { 
            Persist(o, HandleUpdateUsers);
        });

        Command<GetCurrentQuestion>(_ => Sender.Tell(GetCurrentQuestionInfo()));

        Command<Start>(_ =>
        {
            SetStartedState();
            SendNextQuestion();
            SetTimeHandler();
        });
        
        
        Command<HandleAnswerLoop>(_ =>
        {
            var time = _currentQuestionStartTime;
            
            var currentQuestion = CurrentQuestion;
             var everyoneAnswered = CheckEveryoneAnswered();
             
             // Finaliza o round e espera a próxima pergunta (se tiver)
             if (DateTime.Now - time >= TimeSpan.FromSeconds(currentQuestion.TimeLimit) || everyoneAnswered)
             {
                 Timers.Cancel(AnswerloopTimerName);

                 var ranking = GetRanking();
                 Self.Tell(new SendSignalrGroupMessage(_roomIdentifierString,
                     SignalRMessages.RoundFinished, ranking));

                 if (_state.CurrentQuestionIdx >= _state.MaxQuestionIdx)
                 {
                     _roomStateMachine.Fire(RoomTrigger.Finish);

                 }
                 else
                 {
                     _roomStateMachine.Fire(RoomTrigger.WaitForNextQuestion);
                 }
             }
        });

        Command<NextQuestion>(o =>
        {
            if (_state.CurrentQuestionIdx + 1 > _state.MaxQuestionIdx) return; 
            _state.CurrentQuestionIdx+= 1;
            SendNextQuestion();
            SetTimeHandler();
        });
        
        Command<GetOwner>(_ => Sender.Tell(_state.OwnerId));

        Command<SendUserAnswer>(o =>
        { 
            if (!CurrentQuestion.Answers.Select(x => x.Id).Contains(o.AnswerId)) return;
            var exists = _state.Answers.TryGetValue(CurrentQuestion.Id, out var question);

            if (!exists) return;
            var answerInfo = CalculatePoints(o.AnswerId);
            question!.TryAdd(o.UserId, answerInfo);
            var user = _state.CurrentUsers.FirstOrDefault(x => x.Id == o.UserId);
            if (user != null) user.Answered = true;
            Self.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.UserAnswered, o.UserId));
        });

        Command<GetCurrentUsers>(_ =>
        {
            Sender.Tell(_state.CurrentUsers);
        });
        
        Command<UserConnected>(o =>
        {
            Self.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
        });
        
                
        Command<ShutdownCommand>(_ =>
        {
            Self.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.RoomDeleted, true));
            DeleteMessages(Int64.MaxValue);
            DeleteSnapshots(SnapshotSelectionCriteria.Latest);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomState state)
            {
                _state = state;
                SetStateMachineByCurrentState();
            }
        });
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });

        Command<DeleteSnapshotsSuccess>(o =>
        {
            if (o.Criteria.Equals(SnapshotSelectionCriteria.Latest))
            {
                Context.Stop(Self);
            }
        });
        Command<DeleteMessagesSuccess>(_ => { });
    }

    private CurrentQuestionInfo GetCurrentQuestionInfo()
    {
        return new CurrentQuestionInfo(_state.Template.Questions[_state.CurrentQuestionIdx], _state.CurrentQuestionIdx + 1, _state.MaxQuestionIdx + 1);
    }


    private ImmutableArray<UserRanking> GetRanking()
    {

        var answered = _state.Answers.Select(x => x.Value)
            .SelectMany(x => x)
            .GroupBy(x => x.Key)
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
                Name = _state.CurrentUsers.FirstOrDefault(y => y.Id == x.Id)?.Name!,
                Rank = i + 1
            }).ToArray();


        var usersWithAnswers = _state.Answers.Select(x => x.Value)
            .SelectMany(x => x)
            .Select(x => x.Key)
            .ToHashSet();

            var unanswered = _state.CurrentUsers
            .Where(x => !usersWithAnswers.Contains(x.Id) && x.Id != _state.OwnerId)
            .Select(x => new UserRanking
        {
            Id = x.Id,
            AverageTime = 0,
            Name = x.Name,
            Points = 0,
            Rank = null
        });


        return answered.Concat(unanswered).OrderBy(x => x.Rank == null).ThenBy(x => x.Rank)
            .ToImmutableArray();
    }
    
    private void SendNextQuestion()
    {
         _roomStateMachine.Fire(RoomTrigger.DisplayQuestion);
         foreach (var user in _state.CurrentUsers)
         {
             user.Answered = false;
         }
         Self.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.NextQuestion,
             GetCurrentQuestionInfo()));
    }
    
    private void SetTimeHandler()
    {
        _currentQuestionStartTime = DateTime.Now;
        Timers.StartPeriodicTimer(AnswerloopTimerName, HandleAnswerLoop.Instance
             , TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
    }

    private void SetStateMachineByCurrentState()
    {
        _roomStateMachine = RoomBehavior.GetStateMachine(_state.CurrentState);
    }
    
     private void SetupStateTriggers()
     {
         SetStateMachineByCurrentState();

         _roomStateMachine.OnTransitionCompleted((transition) =>
         {
             _state.CurrentState = transition.Destination;
             Persist(transition.Destination, o =>
             {
                 if (transition.Destination is not RoomStatus.DisplayingQuestion)
                 {
                     ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
                 }
                 Self.Tell(new SendSignalrUserMessage(_state.OwnerId,SignalRMessages.RoomStatusChanged, _state.CurrentState));
             });
         });
     }
     
     private RoomAnswer CalculatePoints(Guid answerId)
     {
         var timeToAnswer = DateTime.Now - _currentQuestionStartTime;
         var isCorrect = CurrentQuestion.Answers.Where(x => x.Correct).Select(x => x.Id).Contains(answerId);
         // Kahoot formula: https://support.kahoot.com/hc/en-us/articles/115002303908-How-points-work
         var wouldBePoints = Math.Round((decimal)
             (1 - (timeToAnswer.TotalSeconds /  CurrentQuestion.TimeLimit) / 2) * CurrentQuestion.Points);

         return new RoomAnswer
         {
             AnswerId = answerId,
             Correct = isCorrect,
             Points = isCorrect ? wouldBePoints : 0,
             TimeToAnswer = timeToAnswer
         };
     }

    private record SendSignalrGroupMessage(string GroupId, string MessageName, object Data);
    
    private record SendSignalrUserMessage(string UserId, string MessageName, object Data);
    
    private record HandleAnswerLoop
    {
        private HandleAnswerLoop()
        {
        }

        public static HandleAnswerLoop Instance { get; } = new();
    }


    private void HandleUpdateUsers(UpdateCurrentUsers r)
    {
        var equal = r.Users.SetEquals(_state.CurrentUsers);

        _state.CurrentUsers = r.Users.Select(x => new RoomUser
        {
            Id = x.Id,
            Name = x.Name,
            Owner = x.Id.Equals(_state.OwnerId)
        })
            .OrderByDescending(x => x.Owner)
            .ToHashSet();

        if (!equal)
        {
            Self.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
        }
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    private void HandleSetBase(SetBase r)
    {
        _state.OwnerId = r.OwnerId;
        _state.Template = r.Template;
        _state.Name = r.RoomName;
        _state.MaxQuestionIdx = Math.Clamp(_state.Template.Questions.Count - 1, 0, 100);
        _state.CurrentQuestionIdx = 0;
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }
    
    private void SetStartedState()
     {
         _roomStateMachine.Fire(RoomTrigger.Start);
         _state.Answers.Clear();
         _state.CurrentQuestionIdx = 0;
         foreach (var question in _state.Template.Questions)
         {
             _state.Answers.TryAdd(question.Id, new Dictionary<string, RoomAnswer>());
         }
     }
    
    private bool CheckEveryoneAnswered()
    {
        var users = _state.CurrentUsers.Select(x => x.Id)
            .Where(x => x != _state.OwnerId)
            .ToHashSet();
        var currentQuestion = CurrentQuestion;
        // verifica se para cada usuário logado, tirando o host, existe um registro de resposta, de maneira burra
        return _state.Answers[currentQuestion.Id].Keys.ToHashSet().SetEquals(users);
    }


    public static Props Props(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        return Akka.Actor.Props.Create(() => new RoomActor(roomIdentifier, hubContext, userService));
    }


    public ITimerScheduler Timers { get; set; } = null!;
}

public record SetBase(string RoomName, string OwnerId, Template Template);

public record GetCurrentState
{
    private GetCurrentState()
    {
    }

    public static GetCurrentState Instance { get; } = new();
}

public record GetSummary
{
    private GetSummary()
    {
    }

    public static GetSummary Instance { get; } = new();
}

public record UpdateCurrentUsers(HashSet<UserInfo> Users);

public record GetCurrentQuestion
{
    private GetCurrentQuestion()
    {
    }

    public static GetCurrentQuestion Instance { get; } = new();
}

public record GetCurrentUsers
{
    private GetCurrentUsers()
    {
    }

    public static GetCurrentUsers Instance { get; } = new();
}

public record Start
{
    private Start()
    {
    }

    public static Start Instance { get; } = new();
}

public record NextQuestion
{
    private NextQuestion()
    {
    }

    public static NextQuestion Instance { get; } = new();
}

public record GetOwner
{
    private GetOwner()
    {
    }

    public static GetOwner Instance { get; } = new();
}

public record SendUserAnswer(string UserId, Guid AnswerId);