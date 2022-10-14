using System.Buffers;
using System.Collections.Immutable;
using System.Security.Claims;
using Orleans;
using Shared.Grains;
using Shared.Grains.Games;
using Shared.Models;

namespace Kanelson.Services;

public class GameService : IGameService
{
    private readonly IGrainFactory _client;
    private readonly string _currentUser;


    public GameService(IGrainFactory client, IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        _client = client;
    }

    public async Task UpsertGame(Game game)
    {
        var manager = _client.GetGrain<IGameManagerGrain>(_currentUser);
        var gameGrain = _client.GetGrain<IGameGrain>(game.Id);
        await gameGrain.SetBase(game, _currentUser);
        await manager.RegisterAsync(gameGrain.GetPrimaryKey());
    }

    public async Task<ImmutableArray<GameSummary>> GetGames()
    {
        var manager = _client.GetGrain<IGameManagerGrain>(_currentUser);
        var keys = await manager.GetAllAsync();
        // fan out to get the individual items from the cluster in parallel
        var tasks = ArrayPool<Task<GameSummary>>.Shared.Rent(keys.Length);
        try
        {
            // issue all individual requests at the same time
            for (var i = 0; i < keys.Length; ++i)
            {
                tasks[i] = _client.GetGrain<IGameGrain>(keys[i]).GetSummary();
            }

            // build the result as requests complete
            var result = ImmutableArray.CreateBuilder<GameSummary>(keys.Length);
            for (var i = 0; i < keys.Length; ++i)
            {
                var item = await tasks[i];
                
                result.Add(item);
            }
            return result.ToImmutableArray();
        }
        finally
        {
            ArrayPool<Task<GameSummary>>.Shared.Return(tasks);
        }
    }

    public async Task<Game> GetGame(Guid id)
    {
        var manager = _client.GetGrain<IGameManagerGrain>(_currentUser);
        var games = await manager.GetAllAsync();
        if (!games.Contains(id))
        {
            throw new KeyNotFoundException();
        }

        return await _client.GetGrain<IGameGrain>(id).GetGame();
    }

    public async Task DeleteGame(Guid id)
    {
        var manager = _client.GetGrain<IGameManagerGrain>(_currentUser);
        var games = await manager.GetAllAsync();
        if (!games.Contains(id))
        {
            throw new KeyNotFoundException();
        }

        var grain = _client.GetGrain<IGameGrain>(id);
        await grain.Delete();
        await manager.UnregisterAsync(id);

    }
}

public interface IGameService
{
    Task UpsertGame(Game game);
    Task<ImmutableArray<GameSummary>> GetGames();
    
    Task<Game> GetGame(Guid id);
    Task DeleteGame(Guid id);
}