using Akka.Actor;
using Akka.Hosting;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Local;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Threading.Channels;
using System.Timers;

namespace Kanelson.Pages.Rooms;

public sealed partial class RoomPage : ComponentBase, IAsyncDisposable
{

    [Parameter] 
    public string Id { get; set; } = null!;
        
    [Inject] 
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IStringLocalizer<Localization.Shared> Loc { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    
    [Inject]
    private IRoomService RoomService { get; set; } = null!;
    
    [Inject]
    private IUserService UserService { get; set; } = null!;

    [Inject]
    private IRequiredActor<LocalRoomActorManager> LocalRoomActorManager { get; set; } = null!;

    private RoomSummary? _summary;
    private string? _roomUrl;
    private HashSet<RoomUser> _connectedUsers = new();
    private readonly TimerConfiguration _timerConfig = new();
    private CancellationTokenSource? _cts;
    private IActorRef? _localRoomActor;
    private Guid _subscriptionId;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            _summary = await RoomService.Get(Id);
            if (_summary.Owner.Id == UserService.CurrentUser)
                _roomUrl = Navigation.BaseUri.TrimEnd('/') + "/room/" + Id;
            var userInfo = await UserService.GetUserInfo(UserService.CurrentUser);
            _localRoomActor = await LocalRoomActorManager.ActorRef.Ask<IActorRef>(new GetLocalRoom(Id));
            var subscription = await _localRoomActor.Ask<SubscriptionResult>(
                new SubscribeToRoom(Id, userInfo.Id, userInfo.Name));
            _subscriptionId = subscription.SubscriptionId;
            _cts = new CancellationTokenSource();
            _ = RunDataPump(subscription.Reader, _cts.Token);
            _timerConfig.SetupAction(TimeElapsed);

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

    private async Task RunDataPump(ChannelReader<IRoomEvent> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var roomEvent in reader.ReadAllAsync(cancellationToken))
            {
                switch (roomEvent)
                {
                    case RoomEvents.CurrentUsersUpdated users:
                        _connectedUsers = users.Users;
                        break;
                    case RoomEvents.UserAnswered answered:
                        var user = _connectedUsers.FirstOrDefault(x =>
                            string.Equals(x.Id, answered.UserId, StringComparison.OrdinalIgnoreCase));
                        if (user is not null)
                        {
                            user.Answered = true;
                        }
                        break;
                    case RoomEvents.RoomDeleted:
                        Snackbar.Add(Loc["RoomDeleted"], Severity.Warning);
                        Navigation.NavigateTo("rooms");
                        break;
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void TimeElapsed(object? sender, ElapsedEventArgs e)
    {
        _timerConfig.Increment();
        InvokeAsync(StateHasChanged);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _localRoomActor?.Tell(new UnsubscribeFromRoom(Id, _subscriptionId));

        if (_timerConfig is IDisposable handle)
        {
            handle.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
