using Orleans;
using Shared.Models;

namespace Shared.Grains.Rooms;

public interface IRoomGrain : IGrainWithStringKey
{

    Task SetBase(string roomName, string owner, Template template);
    Task<RoomSummary> GetSummary();
}