using System.Collections.Immutable;
using System.Timers;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Hubs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using Timer = System.Timers.Timer;

namespace Kanelson.Pages.Rooms;

public partial class Player : BaseRoomPage
{

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    
    private PlayerStatus _playerStatus = PlayerStatus.Answering;
    
    private async Task Answer(Guid alternativeId)
    {
        TimerConfiguration.Stop();
        _playerStatus = PlayerStatus.Answered;
        await HubConnection!.SendAsync(SignalRMessages.Answer, RoomId,  alternativeId);
        await InvokeAsync(StateHasChanged);
    }

    protected override void ConfigureExtraSignalrEvents()
    {
        base.ConfigureExtraSignalrEvents();

        HubConnection.On<UserAnswerSummary>(SignalRMessages.RoundSummary, summary =>
        {
            // TODO: Exibir essa informação de alguma maneira

            InvokeAsync(StateHasChanged);
        });

        HubConnection.On<RejectionReason>(SignalRMessages.AnswerRejected, reason =>
        {
            var stringReason = reason is RejectionReason.InvalidState
                ? Loc["InvalidState"]
                : Loc["AnswerRejected"];
            if (reason is RejectionReason.InvalidAlternatives)
            {
                _playerStatus = PlayerStatus.Answering;
            }
            Snackbar.Add(stringReason, Severity.Error, config =>
            {
                config.RequireInteraction = false;
                config.CloseAfterNavigation = false;
                config.ShowCloseIcon = false;
            });
            InvokeAsync(StateHasChanged);
        });
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