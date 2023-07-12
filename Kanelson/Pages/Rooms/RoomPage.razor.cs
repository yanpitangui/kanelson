using Kanelson.Actors.Rooms;
using Kanelson.Hubs;
using Kanelson.Models;
using Kanelson.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Timers;

namespace Kanelson.Pages.Rooms;

public sealed partial class RoomPage : ComponentBase, IAsyncDisposable
{

    [Parameter]
    public string Id { get; set; }
        
    [Inject] 
    private NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    private IHttpContextAccessor HttpAccessor { get; set; } = null!;
    
    
    [Inject]
    private IStringLocalizer<Localization.Shared> Loc { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    
    [Inject]
    private IRoomService RoomService { get; set; } = null!;
    
    [Inject]
    private IUserService UserService { get; set; } = null!;
    
    
    private RoomSummary? _summary;
    private HashSet<RoomUser> _connectedUsers = new();
    private HubConnection _hubConnection = null!;
    private readonly TimerConfiguration _timerConfig = new();


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _summary = await RoomService.Get(Id);
            
            _hubConnection = HttpAccessor.GetConnection(Navigation, "roomHub");
            ConfigureCommonSignalrEvents();

        }
        catch (Exception)
        {
            Snackbar.Add(Loc["RoomNotFound"], Severity.Error, config =>
            {
                config.RequireInteraction = false;
                config.CloseAfterNavigation = false;
                config.ShowCloseIcon = false;
            });
            Navigation.NavigateTo("rooms");
        }
    }
    
    private void ConfigureCommonSignalrEvents()
    {
        
        _timerConfig.SetupAction(TimeElapsed);

        
        _hubConnection.On<HashSet<RoomUser>>(RoomHub.SignalRMessages.CurrentUsersUpdated, (users) =>
        {
            _connectedUsers = users;
            InvokeAsync(StateHasChanged);
        });
        
        
        _hubConnection.On<string>(RoomHub.SignalRMessages.UserAnswered, (userId) =>
        {
            var user = _connectedUsers.FirstOrDefault(x => string.Equals(x.Id, userId, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                user.Answered = true;
            }
        });

        _hubConnection.On<bool>(RoomHub.SignalRMessages.RoomDeleted, _ =>
        {
            Snackbar.Add(Loc["RoomDeleted"], Severity.Warning);
            Navigation.NavigateTo("rooms");
        });
    }
    
    private void TimeElapsed(object? sender, ElapsedEventArgs e)
    {
        _timerConfig.Increment();
        InvokeAsync(StateHasChanged);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        if (_timerConfig is IDisposable handle)
        {
            handle.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}