    using System.Collections.Immutable;
    using Akka.Actor;
    using Akka.Persistence;
    using Kanelson.Hubs;
    using Kanelson.Models;
    using Kanelson.Services;
    using Microsoft.AspNetCore.SignalR;

    namespace Kanelson.Actors.Rooms;

    public class RoomActor : BaseWithSnapshotFrequencyActor, IWithTimers
    {
        private const string AnswerloopTimerName = "AnswerLoop";
        private readonly long _roomIdentifier;

        public override string PersistenceId { get; }

        private RoomState _state;

        private readonly IActorRef _signalrActor;
        
        
        private DateTime _currentQuestionStartTime;
        private readonly string _roomIdentifierString;
        private TemplateQuestion CurrentQuestion => _state.Template.Questions[_state.CurrentQuestionIdx];

        private IEnumerable<RoomUser> Users => _state.CurrentUsers.Where(x => !x.Owner);


        public RoomActor(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
        {
            _roomIdentifier = roomIdentifier;
            _roomIdentifierString = _roomIdentifier.ToString();
            PersistenceId = $"room-{roomIdentifier}";
            
            _signalrActor = Context.ActorOf(SignalrActor.Props((IHubContext) hubContext));
            
            _state = new RoomState();
            
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

                     _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.RoundFinished, true));

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
                         _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.RoomFinished,
                             GetRanking()));
                     }

                 }
            });

            Command<NextQuestion>(o =>
            {
                if (_state.CurrentQuestionIdx + 1 > _state.MaxQuestionIdx || _state.CurrentState == RoomStatus.DisplayingQuestion) return; 
                _state.CurrentQuestionIdx+= 1;
                SendNextQuestion();
                SetTimeHandler();
            });
            
            Command<GetOwner>(_ => Sender.Tell(_state.OwnerId));

            Command<SendUserAnswer>(o =>
            {
                var possibleAlternatives = CurrentQuestion.Alternatives.Select(static x => x.Id);
                if (!o.AlternativeIds.All(x => possibleAlternatives.Contains(x)))
                {
                    // TODO: Escutar no front caso a resposta seja rejeitada
                    _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.AnswerRejected));
                    return;
                }
                var exists = _state.Answers.TryGetValue(CurrentQuestion.Id, out var question);

                if (!exists) return;
                var alternativeInfo = CalculatePoints(o.AlternativeIds);
                question!.TryAdd(o.UserId, alternativeInfo);
                var user = _state.CurrentUsers.FirstOrDefault(x => string.Equals(x.Id, o.UserId, StringComparison.OrdinalIgnoreCase));
                if (user != null) user.Answered = true;
                _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.UserAnswered, o.UserId));
            });
            
            Command<UserConnected>(o =>
            {
                _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
                if(_state.CurrentState == RoomStatus.Finished) 
                    _signalrActor.Tell(new SendSignalrUserMessage(o.UserId, SignalRMessages.RoomFinished, GetRanking()));
            });
            
                    
            Command<ShutdownCommand>(_ =>
            {
                _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.RoomDeleted, true));
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
            var usersWithAnswers = _state.Answers.Select(x => x.Value)
                .SelectMany(x => x)
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
            var alternatives = CurrentQuestion.Alternatives;
            var userAnswer = _state.Answers[CurrentQuestion.Id][userId];
            return new UserAnswerSummary(CurrentQuestion.Name, alternatives, userAnswer.Alternatives);
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
        
        private void SendNextQuestion()
        {
            UpdateState(RoomStatus.DisplayingQuestion);
            foreach (var user in _state.CurrentUsers)
            {
                user.Answered = false;
            }
            _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.NextQuestion,
                GetCurrentQuestionInfo()));
        }
        
        private void SetTimeHandler()
        {
            _currentQuestionStartTime = DateTime.Now;
            Timers.StartPeriodicTimer(AnswerloopTimerName, HandleAnswerLoop.Instance
                 , TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        }



        private void UpdateState(RoomStatus destination)
        {
            _state.CurrentState = destination;
            Persist(destination, o =>
            {
                if (o is not RoomStatus.DisplayingQuestion)
                {
                    SaveSnapshotIfPassedInterval(_state);
                }
                _signalrActor.Tell(new SendSignalrUserMessage(_state.OwnerId,SignalRMessages.RoomStatusChanged, _state.CurrentState));
            });
        }
         
         private RoomAnswer CalculatePoints(Guid[] alternativeIds)
         {
             var timeToAnswer = DateTime.Now - _currentQuestionStartTime;
             
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

             var percentage = wrong >= correct ? 0 : (decimal)maxCorrect / ((decimal)correct - (decimal)wrong); 
                 
             
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
        
        private sealed record HandleAnswerLoop
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
                _signalrActor.Tell(new SendSignalrGroupMessage(_roomIdentifierString, SignalRMessages.CurrentUsersUpdated, _state.CurrentUsers));
            }
            SaveSnapshotIfPassedInterval(_state);
        }

        private void HandleSetBase(SetBase r)
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
         }
        
        private bool CheckEveryoneAnswered()
        {
            var users = Users.Select(x => x.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            // verifica se para cada usuário logado, tirando o host, existe um registro de resposta, de maneira burra
            var isEqual = users.SetEquals(_state.Answers[CurrentQuestion.Id].Keys);
            return isEqual;
        }


        public static Props Props(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
        {
            return Akka.Actor.Props.Create<RoomActor>(roomIdentifier, hubContext, userService);
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

    public record SendUserAnswer(string UserId, Guid[] AlternativeIds);