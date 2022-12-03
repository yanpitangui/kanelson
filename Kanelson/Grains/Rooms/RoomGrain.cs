using System.Collections.Concurrent;
using System.Collections.Immutable;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Kanelson.Contracts.Grains.Rooms;
using Kanelson.Contracts.Models;

namespace Kanelson.Grains.Rooms;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;
    private readonly IUserService _userService;
    private readonly IHubContext<RoomHub> _hubContext;
    private IDisposable? _timerHandler;
    private DateTime _currentQuestionStartTime;
    private RoomStateMachine _roomStateMachine = null!;

    private TemplateQuestion CurrentQuestion => _state.State.Template.Questions[_state.State.CurrentQuestionIdx];
    
    public RoomGrain([PersistentState("rooms", "kanelson-storage")]
        IPersistentState<RoomState> users, IUserService userService,
        IHubContext<RoomHub> hubContext)
    {
        _state = users;
        _userService = userService;
        _hubContext = hubContext;
    }

    private void SetupStateTriggers()
    {
        _roomStateMachine = RoomBehavior.GetStateMachine(_state.State.CurrentState);

        _roomStateMachine.OnTransitionCompletedAsync(async (transition) =>
        {
            _state.State.CurrentState = transition.Destination;
            if (transition.Destination is not RoomStatus.DisplayingQuestion)
            {
                await _state.WriteStateAsync();
            }
            await _hubContext.Clients
                .User(await this.GetOwner()).SendAsync("RoomStateChanged", _state.State.CurrentState);
        });
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        SetupStateTriggers();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task SetBase(string roomName, string owner, Template template)
    {
        _state.State.Name = roomName;
        _state.State.OwnerId = owner;
        _state.State.Template = template;
        _state.State.MaxQuestionIdx = Math.Clamp(template.Questions.Length - 1, 0, int.MaxValue);
        _state.State.CurrentQuestionIdx = 0;
        await _state.WriteStateAsync();
    }

    public async Task<RoomSummary> GetSummary()
    {
        var owner = await _userService
            .GetUserInfo(_state.State.OwnerId);

        return new RoomSummary(this.GetPrimaryKeyString(), _state.State.Name, owner, _state.State.CurrentState);
    }

    public async Task UpdateCurrentUsers(HashSet<UserInfo> users)
    {
        var equal = users.SetEquals(_state.State.CurrentUsers);
        _state.State.CurrentUsers = users;
        await _state.WriteStateAsync();
        if (!equal)
        {
            await _hubContext.Clients.Group(this.GetPrimaryKeyString()).SendAsync("CurrentUsersUpdated", users);
            await _hubContext.Clients.User(await this.GetOwner()).SendAsync("CurrentUsersUpdated", users);
        }
    }

    public Task<HashSet<UserInfo>> GetCurrentUsers()
    {
        return Task.FromResult(_state.State.CurrentUsers);
    }

    public Task<TemplateQuestion> GetCurrentQuestion()
    {
        return Task.FromResult(CurrentQuestion);
    }

    public Task<RoomStatus> GetCurrentState()
    {
        return Task.FromResult(_state.State.CurrentState);
    }


    public async Task<bool> NextQuestion()
    {
        if (_state.State.CurrentQuestionIdx + 1 > _state.State.MaxQuestionIdx) return false;
        _state.State.CurrentQuestionIdx+= 1;
        await SendNextQuestion();
        SetTimeHandler();
        return true;

    }

    private void SetTimeHandler()
    {
        // Evitar que fique em loop infinito caso seja chamado mais de uma vez
        _timerHandler?.Dispose();
        _currentQuestionStartTime = DateTime.Now;
        _timerHandler = RegisterTimer(WaitForAnswersOrTimeOut, _currentQuestionStartTime, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1));
    }

    public async Task Start()
    {
        await SetStartedState();
        await SendNextQuestion();
        SetTimeHandler();
    }

    private async Task SendNextQuestion()
    {
        await _roomStateMachine.FireAsync(RoomTrigger.DisplayQuestion);
        await _hubContext.Clients.Group(this.GetPrimaryKeyString())
            .SendAsync("NextQuestion", CurrentQuestion);
    }


    private async Task SetStartedState()
    {
        await _roomStateMachine.FireAsync(RoomTrigger.Start);
        _state.State.Answers.Clear();
        _state.State.CurrentQuestionIdx = 0;
        foreach (var question in _state.State.Template.Questions)
        {
            _state.State.Answers.TryAdd(question.Id, new ConcurrentDictionary<string, RoomAnswer>());
        }
    }

    private bool CheckEveryoneAnswered()
    {
        var users = _state.State.CurrentUsers.Select(x => x.Id).ToHashSet();
        var currentQuestion = CurrentQuestion;
        // verifica se para cada usuário logado, existe um registro de resposta, de maneira burra
        return _state.State.Answers[currentQuestion.Id].Keys.ToHashSet().SetEquals(users);
    }

    private async Task WaitForAnswersOrTimeOut(object initialTime)
    {
        var time = Convert.ToDateTime(initialTime);
        var currentQuestion = CurrentQuestion;
        var everyoneAnswered = CheckEveryoneAnswered();
        
        // Finaliza o round e espera a próxima pergunta (se tiver)
        if (DateTime.Now - time >= TimeSpan.FromSeconds(currentQuestion.TimeLimit) || everyoneAnswered)
        {
            _timerHandler?.Dispose();

            var ranking = GetRanking();
            await _hubContext.Clients
                .Group(this.GetPrimaryKeyString())
                .SendAsync("RoundFinished", ranking);

            await _roomStateMachine.FireAsync(RoomTrigger.WaitForNextQuestion);
        }
    }

    private ImmutableArray<UserRanking> GetRanking()
    {
        var groupedData = _state.State.Answers.Select(x => x.Value)
            .SelectMany(x => x)
            .GroupBy(x => x.Key)
            .Select(x => new
            {
                Id = x.Key,
                Points = x.Sum(y => y.Value.Points),
                Average = x.Average(y => (decimal)y.Value.TimeToAnswer.TotalSeconds)
            })
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.Average)
            .Select((x, i) => new UserRanking
            {
                Id = x.Id,
                AverageTime = x.Average,
                Points = x.Points,
                // talvez trocar isso aqui se tiver muito lerdo
                Name = _state.State.CurrentUsers.FirstOrDefault(y => y.Id == x.Id)?.Name!,
                Rank = i + 1
            })
            .ToImmutableArray();
        return groupedData;
    }

    public Task<string> GetOwner()
    {
        return Task.FromResult(_state.State.OwnerId);
    }

    public async Task Delete()
    {
        await _state.ClearStateAsync();
        DeactivateOnIdle();
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
    
    public Task Answer(string userId, string roomId, Guid answerId)
    {

        if (!CurrentQuestion.Answers.Select(x => x.Id).Contains(answerId)) return Task.CompletedTask;
        var question =
            _state.State.Answers.GetOrAdd(CurrentQuestion.Id,
                _ => new ConcurrentDictionary<string, RoomAnswer>());
        
        var answerInfo = CalculatePoints(answerId);
        
        question.TryAdd(userId, answerInfo);
        return Task.CompletedTask;
    }
}
