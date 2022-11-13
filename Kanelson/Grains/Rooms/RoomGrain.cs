using System.Collections.Concurrent;
using System.Collections.Immutable;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
using Orleans;
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

    private TemplateQuestion CurrentQuestion => _state.State.Template.Questions[_state.State.CurrentQuestionIdx];


    public RoomGrain([PersistentState("rooms", "kanelson-storage")]
        IPersistentState<RoomState> users, IUserService userService,
        IHubContext<RoomHub> hubContext)
    {
        _state = users;
        _userService = userService;
        _hubContext = hubContext;
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

        return new RoomSummary(this.GetPrimaryKeyString(), _state.State.Name, owner, _state.State.Status);
    }

    public async Task UpdateCurrentUsers(HashSet<UserInfo> users)
    {
        var equal = users.SetEquals(_state.State.CurrentUsers);
        _state.State.CurrentUsers = users;
        await _state.WriteStateAsync();
        if (!equal)
        {
            await _hubContext.Clients.Group(this.GetPrimaryKeyString()).SendAsync("CurrentUsersUpdated", users);
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
        _currentQuestionStartTime = DateTime.Now;
        _timerHandler = RegisterTimer(WaitForAnswersOrTimeOut, _currentQuestionStartTime, TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1));
    }

    public async Task<bool> Start()
    {
        //if (_state.State.Status != RoomStatus.Created) return Task.FromResult(false);
        SetStartedState();
        await SendNextQuestion();
        SetTimeHandler();
        return true;
    }

    private async Task SendNextQuestion()
    {
        await _hubContext.Clients.Group(this.GetPrimaryKeyString())
            .SendAsync("NextQuestion", CurrentQuestion);
    }


    private void SetStartedState()
    {
        _state.State.Status = RoomStatus.Started;
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
        if (DateTime.Now - time >= TimeSpan.FromSeconds(currentQuestion.TimeLimit) || everyoneAnswered)
        {
            _timerHandler?.Dispose();

            var ranking = GetRanking();
            await _hubContext.Clients
                .Group(this.GetPrimaryKeyString())
                .SendAsync("RoundFinished", ranking);

            await _state.WriteStateAsync();
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

[GenerateSerializer]
public record RoomState
{
    [Id(0)]
    public string OwnerId { get; set; } = null!;
    
    [Id(1)]
    public string Name { get; set; } = null!;

    [Id(2)]
    public ConcurrentDictionary<Guid, ConcurrentDictionary<string, RoomAnswer>> Answers { get; init; } = new();

    [Id(3)]
    public Template Template { get; set; } = null!;
    
    [Id(3)]
    public RoomStatus Status { get; set; }

    [Id(4)]
    public HashSet<UserInfo> CurrentUsers { get; set; } = new();
    
    [Id(5)]
    public int CurrentQuestionIdx { get; set; }
    
    [Id(6)]
    public int MaxQuestionIdx { get; set; }
}

public record RoomAnswer
{
    public Guid AnswerId { get; init; }

    public TimeSpan TimeToAnswer { get; init; } = new();
    
    public decimal Points { get; init; }
    public bool Correct { get; set; }
}

public record UserRanking : UserInfo
{
    public decimal Points { get; set; }
    
    public decimal AverageTime { get; set; }
    
    public int Rank { get; set; }
}
