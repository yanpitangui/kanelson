using System.Collections.Immutable;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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
        await RoomService.Answer(RoomId, alternativeId);
        await InvokeAsync(StateHasChanged);
    }

    protected override void HandleEvent(IRoomEvent roomEvent)
    {
        base.HandleEvent(roomEvent);
        switch (roomEvent)
        {
            case RoomEvents.UserRoundSummary:
                break;
            case RoomEvents.AnswerRejected rejection:
                var stringReason = rejection.Reason is RejectionReason.InvalidState
                    ? Loc["InvalidState"]
                    : Loc["AnswerRejected"];
                if (rejection.Reason is RejectionReason.InvalidAlternatives)
                {
                    _playerStatus = PlayerStatus.Answering;
                }
                Snackbar.Add(stringReason, Severity.Error, config =>
                {
                    config.RequireInteraction = false;
                    config.CloseAfterNavigation = false;
                    config.ShowCloseIcon = false;
                });
                break;
        }
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
