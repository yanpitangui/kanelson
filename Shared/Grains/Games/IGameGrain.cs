using Orleans;
using Shared.Models;

namespace Shared.Grains.Games;

public interface IGameGrain : IGrainWithGuidKey
{
    public Task SetBase(Game game, string ownerId);
    Task<string> GetOwner();
    Task<GameSummary> GetSummary();
    Task<Game> GetGame();
    Task Delete();
}