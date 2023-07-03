using System.Collections.Immutable;
using Kanelson.Actors.Rooms;
using Kanelson.Hubs;
using Kanelson.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Kanelson.Pages.Rooms;

public class BaseRoomPage : MudComponentBase
{
    
    [Parameter]
    public long RoomId { get; set; }

    
    [Parameter]
    public HubConnection HubConnection { get; set; } = null!;
    
    [Parameter]
    public TimerConfiguration TimerConfiguration { get; set; } = null!;
    
    [Inject]
    protected IStringLocalizer<Localization.Shared> Loc { get; set; } = null!;

    protected ImmutableArray<UserRanking>? Rankings;
    
    protected CurrentQuestionInfo? CurrentQuestion;
    
    protected sealed override async Task OnInitializedAsync()
    {

        ConfigureExtraSignalrEvents();
        await HubConnection.StartAsync();

        await HubConnection.SendAsync(RoomHub.SignalRMessages.JoinRoom, RoomId);

        await AfterConnectedConfiguration();
    }

    protected virtual void ConfigureExtraSignalrEvents()
    {
        HubConnection.On<ImmutableArray<UserRanking>>(RoomHub.SignalRMessages.RoomFinished, (ranking) =>
        {
            Rankings = ranking;
            InvokeAsync(StateHasChanged);
        });
        
        HubConnection.On<CurrentQuestionInfo>(RoomHub.SignalRMessages.NextQuestion, (question) =>
        {
            CurrentQuestion = question;
            TimerConfiguration.ResetAndStart(question.Question.TimeLimit);
            OnNextQuestion();
            InvokeAsync(StateHasChanged);
        });
        
        
        HubConnection.On<bool>(RoomHub.SignalRMessages.RoundFinished, (_) =>
        {
            TimerConfiguration.Stop();
            CurrentQuestion = null;
            InvokeAsync(StateHasChanged);
        });
    } 


    protected virtual void OnNextQuestion()
    {
        
    }

    protected virtual Task AfterConnectedConfiguration()
    {
        return Task.CompletedTask;
    }
    
}