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
    private UserAnswerSummary? _roundSummary;
    private readonly HashSet<Guid> _multiCorrectSelections = new();

    private async Task Answer(Guid alternativeId)
    {
        TimerConfiguration.Stop();
        _playerStatus = PlayerStatus.Answered;
        await RoomService.Answer(RoomId, default, alternativeId);
        await InvokeAsync(StateHasChanged);
    }

    private async Task AnswerMulti(Guid[] alternativeIds)
    {
        TimerConfiguration.Stop();
        _playerStatus = PlayerStatus.Answered;
        await RoomService.Answer(RoomId, default, alternativeIds);
        await InvokeAsync(StateHasChanged);
    }

    protected override void HandleEvent(IRoomEvent roomEvent)
    {
        base.HandleEvent(roomEvent);
        switch (roomEvent)
        {
            case RoomEvents.UserRoundSummary summary:
                _roundSummary = summary.Summary;
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

    internal void OnMultiSelectionsChanged(IReadOnlyCollection<Guid> selections)
    {
        _multiCorrectSelections.Clear();
        foreach (var id in selections) _multiCorrectSelections.Add(id);
    }

    protected override void OnNextQuestion()
    {
        _playerStatus = PlayerStatus.Answering;
        _roundSummary = null;
        _multiCorrectSelections.Clear();

        if (CurrentQuestion?.Question.Type == Domain.Questions.QuestionType.MultiCorrect)
        {
            TimerConfiguration.OnExpired = () =>
                _ = AnswerMulti(_multiCorrectSelections.ToArray());
        }
    }

    private string ScoreChipClass(decimal points, int maxPoints)
    {
        if (points <= 0) return "score-chip score-chip--bad";
        if (points >= maxPoints) return "score-chip score-chip--good";
        return "score-chip score-chip--partial";
    }

    private enum PlayerStatus
    {
        Answering,
        Answered
    }
}
