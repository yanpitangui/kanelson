using System.Collections.Immutable;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Models;

namespace Kanelson.Pages.Rooms;

public partial class Admin : BaseRoomPage
{
    private RoomStatus _roomStatus;

    protected override void HandleEvent(IRoomEvent roomEvent)
    {
        base.HandleEvent(roomEvent);
        if (roomEvent is RoomEvents.RoomStatusChanged state)
        {
            _roomStatus = state.Status;
        }
    }

    protected override async Task AfterConnectedConfiguration()
    {
        await base.AfterConnectedConfiguration();
        _roomStatus = await RoomService.GetCurrentState(RoomId);
    }


    private async Task Start()
    {
        await RoomService.Start(RoomId);
    }

    private async Task NextQuestion()
    {
        await RoomService.NextQuestion(RoomId);
    }

    private async Task ExtendTime(int seconds)
    {
        await RoomService.ExtendTime(RoomId, seconds);
    }
}
