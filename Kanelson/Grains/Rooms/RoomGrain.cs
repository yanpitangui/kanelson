using System.Collections.Concurrent;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Shared.Grains.Rooms;
using Shared.Models;

namespace Kanelson.Grains.Rooms;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;
    private readonly IUserService _userService;
    private readonly IHubContext<RoomHub> _hubContext;
    private IDisposable? _timerHandler;

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
        _state.State.MaxQuestionIdx = Math.Min(template.Questions.Length - 1, 0);
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
        return Task.FromResult(_state.State.Template.Questions[_state.State.CurrentQuestionIdx]);
    }

    public async Task<bool> IncrementQuestionIdx()
    {
        if (_state.State.CurrentQuestionIdx + 1 <= _state.State.MaxQuestionIdx)
        {
            _state.State.CurrentQuestionIdx+= 1;
            await _state.WriteStateAsync();
            _timerHandler = RegisterTimer(WaitForAnswersOrTimeOut, DateTime.Now, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
            return true;
        }

        return false;
    }

    public Task<bool> Start()
    {
        if (_state.State.Status != RoomStatus.Created) return Task.FromResult(false);
        SetEmptyQuestions();
        _state.State.Status = RoomStatus.Started;
        
        _timerHandler = RegisterTimer(WaitForAnswersOrTimeOut, DateTime.Now, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
        return Task.FromResult(true);
    }


    private void SetEmptyQuestions()
    {
        foreach (var question in _state.State.Template.Questions)
        {
            _state.State.Answers.GetOrAdd(question.Id, _ => new ConcurrentDictionary<string, RoomAnswer>());
        }
    }

    private bool CheckEveryoneAnswered()
    {
        var users = _state.State.CurrentUsers.Select(x => x.Id).ToHashSet();
        var currentQuestion = _state.State.Template.Questions[_state.State.CurrentQuestionIdx];
        // verifica se para cada usuário logado, existe um registro de resposta, de maneira burra
        return _state.State.Answers[currentQuestion.Id].Keys.ToHashSet().SetEquals(users);
    }

    private async Task WaitForAnswersOrTimeOut(object initialTime)
    {
        var time = Convert.ToDateTime(initialTime);
        var currentQuestion = _state.State.Template.Questions[_state.State.CurrentQuestionIdx];
        var everyoneAnswered = CheckEveryoneAnswered();
        if (DateTime.Now - time >= TimeSpan.FromSeconds(currentQuestion.TimeLimit) || everyoneAnswered)
        {
            _timerHandler?.Dispose();
            // Enviar score para todos, com ranking e blabla bla

            await _state.WriteStateAsync();
        }
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
    
}

[Serializable]
public record RoomState
{
    public string OwnerId { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ConcurrentDictionary<Guid, ConcurrentDictionary<string, RoomAnswer>> Answers { get; init; } = new();

    public Template Template { get; set; } = null!;
    
    public RoomStatus Status { get; set; }

    public HashSet<UserInfo> CurrentUsers { get; set; } = new();
    
    public int CurrentQuestionIdx { get; set; }
    
    public int MaxQuestionIdx { get; set; }
}

public record RoomAnswer
{
    public List<Guid> Alternatives { get; set; } = new();

    public TimeSpan TimeToAnswer { get; set; } = new();
}
