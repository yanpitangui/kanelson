using Kanelson.Actors.Rooms;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;

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

    protected HashSet<RoomUser> ConnectedUsers = new();
    
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
        HubConnection.On<HashSet<RoomUser>>(SignalRMessages.CurrentUsersUpdated, (users) =>
        {
            ConnectedUsers = users;
            InvokeAsync(StateHasChanged);
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