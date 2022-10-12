using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson;

public class GameHub : Hub
{
    private readonly IGameService _gameService;
    public GameHub(IGameService gameService)
    {
        _gameService = gameService;
    }



    public async Task StartGame(Guid id)
    {
        await _gameService.StartGame(id);
    }
    
}