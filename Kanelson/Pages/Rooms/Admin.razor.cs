using System.Collections.Immutable;
using Kanelson.Actors.Rooms;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;

namespace Kanelson.Pages.Rooms;

public partial class Admin : BaseRoomPage
{
    [Inject] 
    private IRoomService RoomService {get; set; } = null!;
    
    private RoomStateMachine _roomStateMachine = null!;

    protected override void ConfigureSignalrEvents()
    {
        base.ConfigureSignalrEvents();
        HubConnection.On<RoomStatus>(SignalRMessages.RoomStatusChanged, (state) =>
        {
            _roomStateMachine = RoomBehavior.GetStateMachine(state);
            InvokeAsync(StateHasChanged);
        });
        
        HubConnection.On<CurrentQuestionInfo>(SignalRMessages.NextQuestion, (question) =>
        {
            CurrentQuestion = question;
            InvokeAsync(StateHasChanged);

        });
    }

    protected override async Task AfterConnectedConfiguration()
    {
        var currentState = await RoomService.GetCurrentState(RoomId);
        _roomStateMachine = RoomBehavior.GetStateMachine(currentState);

        ConnectedUsers = await RoomService.GetCurrentUsers(RoomId);

        CurrentQuestion = await RoomService.GetCurrentQuestion(RoomId);

        await InvokeAsync(StateHasChanged);
    }


    private async Task Start()
    {
        await HubConnection.SendAsync(SignalRMessages.Start, RoomId);
    }

    private async Task NextQuestion()
    {
        await RoomService.NextQuestion(RoomId);
    }
}