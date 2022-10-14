using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Hubs;

public class GameHub : Hub
{
    private readonly IGameService _gameService;
    public GameHub(IGameService gameService)
    {
        _gameService = gameService;
    }
    
    
    public async Task Join(Guid id)
    {
    }
    
}