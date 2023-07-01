using System.Collections.Immutable;
using Kanelson.Actors.Rooms;
using Kanelson.Hubs;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;

namespace Kanelson.Pages.Rooms;

public partial class Admin : BaseRoomPage
{
    [Inject] 
    private IRoomService RoomService {get; set; } = null!;

    private RoomStatus _roomStatus;

    protected override void ConfigureSignalrEvents()
    {
        base.ConfigureSignalrEvents();
        HubConnection.On<RoomStatus>(SignalRMessages.RoomStatusChanged, (state) =>
        {
            _roomStatus = state;
            InvokeAsync(StateHasChanged);
        });
    }

    protected override async Task AfterConnectedConfiguration()
    {
        var currentState = await RoomService.GetCurrentState(RoomId);
        _roomStatus = currentState;
        
        CurrentQuestion = await RoomService.GetCurrentQuestion(RoomId);

        await InvokeAsync(StateHasChanged);
    }


    private async Task Start()
    {
        await RoomService.Start(RoomId);
    }

    private async Task NextQuestion()
    {
        await RoomService.NextQuestion(RoomId);
    }
}