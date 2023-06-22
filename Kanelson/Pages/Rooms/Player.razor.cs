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
    private Timer _timerHandle = new(TimeSpan.FromSeconds(1));
    
    private string Format(double percentage)
    {
        return $"{_max - _current}s";
    }
    
    private double _current = 0;
    private double _percentage = 0; 
    private double _max = 0;

    protected override void ConfigureSignalrEvents()
    {
        base.ConfigureSignalrEvents();
        _timerHandle.Elapsed += TimeElapsed;
        HubConnection.On<UserAnswerSummary>(SignalRMessages.RoundFinished, (summary) =>
        {
            _timerHandle.Stop();
            CurrentQuestion = null;
            InvokeAsync(StateHasChanged);
        });

        HubConnection.On<CurrentQuestionInfo>(SignalRMessages.NextQuestion, (question) =>
        {
            CurrentQuestion = question;
            _current = 0;
            _percentage = 100;
            _max = CurrentQuestion.Question.TimeLimit;
            _timerHandle.Start();
            
            _playerStatus = PlayerStatus.Answering;
            InvokeAsync(StateHasChanged);
        });

    }


    private void TimeElapsed(object? sender, ElapsedEventArgs e)
    {
        _current++;
        _percentage = (_max - _current)/_max * 100;
        InvokeAsync(StateHasChanged);
    }

    private async Task Answer(Guid alternativeId)
    {
        _timerHandle.Stop();
        _playerStatus = PlayerStatus.Answered;
        await HubConnection!.SendAsync(SignalRMessages.Answer, RoomId,  alternativeId);
        await InvokeAsync(StateHasChanged);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _timerHandle.Dispose();
    }
    
    
    private enum PlayerStatus
    {
        Answering,
        Answered
    }
}