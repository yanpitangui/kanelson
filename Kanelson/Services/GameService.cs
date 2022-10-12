using System.Security.Claims;
using Orleans;
using Shared.Grains;

namespace Kanelson.Services;

public class GameService : IGameService
{
    private readonly IGrainFactory _grainFactory;
    private readonly string _currentUser;


    public GameService(IGrainFactory grainFactory, IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _grainFactory = grainFactory;
    }

    public async Task<Guid> CreateGame(string name)
    {
        var manager = _grainFactory.GetGrain<IGameManagerGrain>(0);
        var game = _grainFactory.GetGrain<IGameGrain>(Guid.NewGuid());
        await game.SetBase(name, _currentUser);
        await manager.RegisterGame(game.GetPrimaryKey());
        return game.GetPrimaryKey();
    }

    public async Task<bool> JoinGame(Guid id)
    {
        var manager = _grainFactory.GetGrain<IGameManagerGrain>(0);
        if (await manager.RoomExists(id))
        {
            var game = _grainFactory.GetGrain<IGameGrain>(id);
            await game.JoinGame(_currentUser);
            return true;
        }

        return false;
    }

    public async Task GetCurrentQuestion(Guid id)
    {
        
    }

    public async Task AnswerQuestion(Guid roomId, Guid answerId)
    {
        
    }

    public async Task StartGame(Guid id)
    {
        var manager = _grainFactory.GetGrain<IGameManagerGrain>(0);
        if (await manager.RoomExists(id))
        {
            var game = _grainFactory.GetGrain<IGameGrain>(id);
            if (await game.GetOwner() == _currentUser)
            {
                await game.Start();
            }
        }
    }
    
    public async Task GetLastRoundScore(Guid roomId) {}
}

public interface IGameService
{
    Task StartGame(Guid id);
    Task<bool> JoinGame(Guid id);
    Task<Guid> CreateGame(string name);
}