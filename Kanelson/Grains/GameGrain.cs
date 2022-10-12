using Orleans;
using Orleans.Runtime;
using Shared.Grains;
using Shared.Models;

namespace Kanelson.Grains;

public class GameGrain : Grain, IGameGrain
{
    private readonly HashSet<string> _connectedUsers;
    private readonly IPersistentState<GameState> _state;

    public GameGrain(
        [PersistentState("game", "kanelson-storage")]
        IPersistentState<GameState> state)
    {
        _state = state;
        _connectedUsers = new HashSet<string>();
    }

    public async Task SetBase(string name, string ownerId)
    {
        _state.State.Name = name;
        _state.State.OwnerId = ownerId;
        await _state.WriteStateAsync();
    }


    public Task JoinGame(string userId)
    {
        if (userId != _state.State.OwnerId)
        {
            _connectedUsers.Add(userId);
        }
        return Task.CompletedTask;
    }

    public async Task Start()
    {
        if (_state.State.Status == GameStatus.Created)
        {
            _state.State.Status = GameStatus.Started;
            
            // Inicializa as respostas dos usuários logados pra nao ter que fazer isso depois
            var answers = _state.State.Answers;
            foreach (var connectedUser in _connectedUsers)
            {
                answers.Add(connectedUser, new List<UserAnswer>());
            }

            await _state.WriteStateAsync();
        }
    }

    public Task<string> GetOwner()
    {
        return Task.FromResult(_state.State.OwnerId);
    }
}
