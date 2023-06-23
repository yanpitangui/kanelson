using System.Collections.Immutable;
using System.Timers;
using Kanelson.Actors.Rooms;
using Kanelson.Hubs;
using Kanelson.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using Timer = System.Timers.Timer;

namespace Kanelson.Pages.Rooms;

public partial class Player : BaseRoomPage
{

    private PlayerStatus _playerStatus = PlayerStatus.Answering;
    
    private async Task Answer(Guid alternativeId)
    {
        TimerConfig.Stop();
        _playerStatus = PlayerStatus.Answered;
        await HubConnection!.SendAsync(SignalRMessages.Answer, RoomId,  alternativeId);
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnNextQuestion()
    {
        _playerStatus = PlayerStatus.Answering;
    }

    private enum PlayerStatus
    {
        Answering,
        Answered
    }
}