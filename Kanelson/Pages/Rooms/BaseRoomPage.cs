using Kanelson.Actors.Rooms;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Kanelson.Pages.Rooms;

public class BaseRoomPage : ComponentBase, IAsyncDisposable
{
    protected HubConnection HubConnection = null!;
    
    [Parameter]
    public long RoomId { get; set; }
    
    [Inject] 
    protected NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    protected IHttpContextAccessor HttpAccessor { get; set; } = null!;
    
    [Inject]
    protected IStringLocalizer<Localization.Shared> Loc { get; set; } = null!;
    
    [Inject]
    private ISnackbar _snackbar { get; set; } = null!;

    protected List<RoomUser> ConnectedUsers = new();
    
    protected CurrentQuestionInfo? CurrentQuestion;


    protected sealed override async Task OnInitializedAsync()
    {
        HubConnection = HttpAccessor.GetConnection(Navigation);

        ConfigureSignalrEvents();
        
        await HubConnection.StartAsync();

        await HubConnection.SendAsync(SignalRMessages.JoinRoom, RoomId);

        await AfterConnectedConfiguration();
    }

    protected virtual void ConfigureSignalrEvents()
    {
        HubConnection.On<List<RoomUser>>(SignalRMessages.CurrentUsersUpdated, (users) =>
        {
            ConnectedUsers = users;
            InvokeAsync(StateHasChanged);
        });
        
        
        HubConnection.On<string>(SignalRMessages.UserAnswered, (userId) =>
        {
            var idx = ConnectedUsers.FindIndex(x => x.Id == userId);
            ConnectedUsers[idx] = ConnectedUsers[idx] with
            {
                Answered = true
            };
        });

        HubConnection.On<bool>(SignalRMessages.RoomDeleted, _ =>
        {
            _snackbar.Add(Loc["RoomDeleted"], Severity.Warning);
            Navigation.NavigateTo("rooms");
        });
    }

    protected virtual Task AfterConnectedConfiguration()
    {
        return Task.CompletedTask;
    }


    public virtual async ValueTask DisposeAsync()
    {
        if (HubConnection is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}