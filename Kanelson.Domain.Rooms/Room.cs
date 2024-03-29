using Akka.Actor;
using Akka.Persistence;
using Kanelson.Common;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Immutable;

namespace Kanelson.Domain.Rooms;

public class Room : BaseWithSnapshotFrequencyActor, IWithTimers
{
    
    public override string PersistenceId { get; }
    public ITimerScheduler Timers { get; set; } = null!;
    
    private const string AnswerloopTimerName = "AnswerLoop";

    private RoomState _state;

    private readonly IActorRef _signalrActor;
    
    
    private DateTime _currentQuestionStartTime;
    private readonly string _roomIdentifier;
    private TemplateQuestion CurrentQuestion => _state.Template.Questions[_state.CurrentQuestionIdx];

    private IEnumerable<RoomUser> Users => _state.CurrentUsers.Where(x => !x.Owner);


    public Room(string roomIdentifier, IHubContext hubContext, IUserService userService)
    {
        _roomIdentifier = roomIdentifier;
        PersistenceId = $"room-{roomIdentifier}";
        
        _signalrActor = Context.ActorOf(SignalrActor.Props(hubContext));
        
        _state = new RoomState();
        
        Recover<RoomCommands.SetBase>(HandleSetBase);
        
        Command<RoomCommands.SetBase>(o =>
        {
            Persist(o, HandleSetBase);
        });
        
        Command<RoomQueries.GetCurrentState>(_ =>
        {
            Sender.Tell(_state.CurrentState);
        });
        
        CommandAsync<RoomQueries.GetSummary>(async _ =>
        {
            var ownerInfo = await userService.GetUserInfo(_state.OwnerId); 
            var summary = new RoomSummary(roomIdentifier,
                _state.Name,
                ownerInfo);
            Sender.Tell(summary);
        });

        Recover<RoomCommands.UpdateCurrentUsers>(HandleUpdateUsers);

        Command<RoomCommands.UpdateCurrentUsers>(o =>
        { 
            Persist(o, static _ => {});
            // Não bloqueia esperando a persistencia
            HandleUpdateUsers(o);
        });

        Command<RoomQueries.GetCurrentQuestion>(_ => Sender.Tell(GetCurrentQuestionInfo()));

        Command<RoomCommands.Start>(_ =>
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
             if (DateTime.UtcNow - time >= TimeSpan.FromSeconds(currentQuestion.TimeLimit) || everyoneAnswered)
             {
                 Timers.Cancel(AnswerloopTimerName);

                 _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.RoundFinished, Data: currentQuestion));

                 FillAnswersFromUsersThatHaveNotAnswered();
                 
                 foreach (var userId in Users.Select(x => x.Id))
                 {
                     _signalrActor.Tell(new SendSignalrUserMessage(userId, SignalRMessages.RoundSummary, GetUserRoundSummary(userId)));
                 }

                 if (_state.CurrentQuestionIdx < _state.MaxQuestionIdx)
                 {
                     UpdateState(RoomStatus.AwaitingForNextQuestion);
                 }
                 else
                 {
                     UpdateState(RoomStatus.Finished);
                     _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.RoomFinished,
                         GetRanking()));
                 }

             }
        });
        
        Recover<StatusChanged>(o =>
        {
            // Sala não pode ficar num estado de mostrando questão
            if (o.Status == RoomStatus.DisplayingQuestion)
            {
                if (!o.IncrementedQuestionIdx)
                {
                    _state.CurrentState = RoomStatus.Created;
                    // reseta todas as respostas que possivelmente aconteram
                    foreach (var respostas in _state.Answers)
                    {
                        respostas.Value.Clear();
                    }
                }
                else
                {
                    // Retorna round ao estado anterior
                    _state.CurrentState = RoomStatus.AwaitingForNextQuestion;
                    var currentAnswers = _state.Answers[CurrentQuestion.Id];
                    currentAnswers.Clear();
                    _state.CurrentQuestionIdx--;
                }
            }
            else
            {
                _state.CurrentState = o.Status;
            }
        });

        Command<RoomCommands.NextQuestion>(_ =>
        {
            if (_state.CurrentQuestionIdx + 1 > _state.MaxQuestionIdx || _state.CurrentState == RoomStatus.DisplayingQuestion) return; 
            _state.CurrentQuestionIdx+= 1;
            SendNextQuestion(incrementedQuestionIdx: true);
            SetTimeHandler();
        });
        
        Command<RoomQueries.GetOwner>(_ => Sender.Tell(_state.OwnerId));

        Command<RoomCommands.SendUserAnswer>(o =>
        {
            var possibleAlternatives = CurrentQuestion.Alternatives.Select(static x => x.Id);
            if (!Array.TrueForAll(o.AlternativeIds, x => possibleAlternatives.Contains(x)))
            {
                _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.AnswerRejected, RejectionReason.InvalidAlternatives));
                return;
            }

            if (_state.CurrentState is not RoomStatus.DisplayingQuestion)
            {
                _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, 
                    SignalRMessages.AnswerRejected,
                    RejectionReason.InvalidState));
                return;
            }
            var questionAnswers = _state.Answers[CurrentQuestion.Id];
            var alternativeInfo = CalculatePoints(o.AlternativeIds);
            questionAnswers.TryAdd(o.UserId, alternativeInfo);
            var user = _state.CurrentUsers.FirstOrDefault(x => string.Equals(x.Id, o.UserId, StringComparison.OrdinalIgnoreCase));
            if (user != null) user.Answered = true;
            _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.UserAnswered, o.UserId));
        });
        
        Command<RoomCommands.UserConnected>(o =>
        {
            _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
            if(_state.CurrentState == RoomStatus.Finished) 
                _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.RoomFinished, GetRanking()));
        });
        
                
        Command<ShutdownCommand>(_ =>
        {
            _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.RoomDeleted, Data: true));
            DeleteMessages(Int64.MaxValue);
            DeleteSnapshots(SnapshotSelectionCriteria.Latest);
        });
        
        Command<SaveSnapshotSuccess>(_ => { });

        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomState state)
            {
                _state = state;
            }
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
        return new UserAnswerSummary(CurrentQuestion, userAnswer.Alternatives);
    }

    private CurrentQuestionInfo GetCurrentQuestionInfo()
    {
        return new CurrentQuestionInfo(_state.Template.Questions[_state.CurrentQuestionIdx], _state.CurrentQuestionIdx + 1, _state.MaxQuestionIdx + 1);
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
        UpdateState(RoomStatus.DisplayingQuestion, incrementedQuestionIdx);
        foreach (var user in _state.CurrentUsers)
        {
            user.Answered = false;
        }
        _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.NextQuestion,
            GetCurrentQuestionInfo()));
    }
    
    private void SetTimeHandler()
    {
        _currentQuestionStartTime = DateTime.UtcNow;
        Timers.StartPeriodicTimer(AnswerloopTimerName, HandleAnswerLoop.Instance
             , TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
    }



    private void UpdateState(RoomStatus destination, bool incrementedQuestionIdx = false)
    {
        _state.CurrentState = destination;

        Persist(new StatusChanged(destination, incrementedQuestionIdx), _ => {});
        if (destination is RoomStatus.Finished or RoomStatus.Abandoned)
        {
            SaveSnapshot(_state);
        }
        else
        {
            SaveSnapshotIfPassedInterval(_state);
        }
        _signalrActor.Tell(new SendSignalrUserMessage(_state.OwnerId,SignalRMessages.RoomStatusChanged, _state.CurrentState));
        
        
    }
     
     private RoomAnswer CalculatePoints(Guid[] alternativeIds)
     {
         var timeToAnswer = DateTime.UtcNow - _currentQuestionStartTime;
         
         var correctAlternatives = CurrentQuestion
             .Alternatives
             .Where(x => x.Correct)
             .Select(x => x.Id)
             .ToList();

         var maxCorrect = correctAlternatives.Count;
         
         var correct = correctAlternatives
             .Intersect(alternativeIds)
             .Count();


         var wrong = CurrentQuestion.Alternatives
             .Where(x => !x.Correct)
             .Select(x => x.Id)
             .Intersect(alternativeIds)
             .Count();

         var percentage = wrong >= correct ? 0 : maxCorrect / (correct - (decimal)wrong); 
             
         
         var timeMinusDelay = Math.Max(timeToAnswer.TotalSeconds - 0.2d , 0); // Retira 200ms do cálculo para considerar delay  
             
         // Kahoot formula: https://support.kahoot.com/hc/en-us/articles/115002303908-How-points-work
         var wouldBePoints = Math.Round((decimal)
             (1 - ( timeMinusDelay  /  CurrentQuestion.TimeLimit) / 2) * CurrentQuestion.Points);

         return new RoomAnswer
         {
             Alternatives = alternativeIds,
             Points = wouldBePoints * percentage,
             TimeToAnswer = timeToAnswer
         };
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
            _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifier, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
        }
        SaveSnapshotIfPassedInterval(_state);
    }

    private void HandleSetBase(RoomCommands.SetBase r)
    {
        _state.OwnerId = r.OwnerId;
        _state.Template = r.Template;
        _state.Name = r.RoomName;
        _state.MaxQuestionIdx = Math.Clamp(_state.Template.Questions.Count - 1, 0, 100);
        _state.CurrentQuestionIdx = 0;
        SaveSnapshotIfPassedInterval(_state);
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
         SaveSnapshot(_state);
     }
    
    private bool CheckEveryoneAnswered()
    {
        var users = Users.Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        // verifica se para cada usuário logado, tirando o host, existe um registro de resposta, de maneira burra
        var isEqual = users.SetEquals(_state.Answers[CurrentQuestion.Id].Keys);
        return isEqual;
    }


    public static Props Props(string roomIdentifier, IHubContext hubContext, IUserService userService)
    {
        return Akka.Actor.Props.Create<Room>(roomIdentifier, hubContext, userService);
    }
    
    
    private sealed record HandleAnswerLoop
    {
        private HandleAnswerLoop()
        {
        }

        public static HandleAnswerLoop Instance { get; } = new();
    }

    private sealed record StatusChanged(RoomStatus Status, bool IncrementedQuestionIdx);




}

public enum RejectionReason
{
    InvalidState,
    InvalidAlternatives
}