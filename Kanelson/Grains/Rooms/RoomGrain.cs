using Kanelson.Services;
using Orleans;
using Orleans.Runtime;
using Shared.Grains.Rooms;
using Shared.Models;

namespace Kanelson.Grains.Rooms;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;
    private readonly IUserService _userService;

    public RoomGrain([PersistentState("rooms", "kanelson-storage")]
        IPersistentState<RoomState> users, IUserService userService)
    {
        _state = users;
        _userService = userService;
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
        _state.State.CurrentUsers = users;
        await _state.WriteStateAsync();
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
            return true;
        }

        return false;
    }

    public async Task<bool> Start()
    {
        if (_state.State.Status != RoomStatus.Created) return false;
        _state.State.Status = RoomStatus.Started;
        await _state.WriteStateAsync();
        return true;

    }

    public Task<string> GetOwner()
    {
        return Task.FromResult(_state.State.OwnerId);
    }
}

[Serializable]
public record RoomState
{
    public string OwnerId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Template Template { get; set; } = null!;
    
    public RoomStatus Status { get; set; }

    public HashSet<UserInfo> CurrentUsers { get; set; } = new();
    
    public int CurrentQuestionIdx { get; set; }
    
    public int MaxQuestionIdx { get; set; }
}
