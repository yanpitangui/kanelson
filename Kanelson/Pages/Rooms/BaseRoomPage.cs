using System.Collections.Immutable;
using System.Threading.Channels;
using Akka.Actor;
using Akka.Hosting;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Local;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Kanelson.Pages.Rooms;

public abstract class BaseRoomPage : MudComponentBase, IAsyncDisposable
{
    [Parameter]
    public string RoomId { get; set; } = null!;

    [Parameter]
    public TimerConfiguration TimerConfiguration { get; set; } = null!;

    [Inject]
    protected IStringLocalizer<Localization.Shared> Loc { get; set; } = null!;

    [Inject]
    protected IRoomService RoomService { get; set; } = null!;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected IRequiredActor<LocalRoomActorManager> LocalRoomActorManager { get; set; } = null!;

    protected ImmutableArray<UserRanking>? Rankings;
    protected CurrentQuestionInfo? CurrentQuestion;

    private CancellationTokenSource? _cts;
    private IActorRef? _localRoomActor;
    private Guid _subscriptionId;

    protected sealed override async Task OnInitializedAsync()
    {
        var userInfo = await UserService.GetUserInfo(UserService.CurrentUser);
        _localRoomActor = await LocalRoomActorManager.ActorRef.Ask<IActorRef>(new GetLocalRoom(RoomId));
        var subscription = await _localRoomActor.Ask<SubscriptionResult>(
            new SubscribeToRoom(RoomId, userInfo.Id, userInfo.Name));
        _subscriptionId = subscription.SubscriptionId;
        _cts = new CancellationTokenSource();
        _ = RunDataPump(subscription.Reader, _cts.Token);

        await AfterConnectedConfiguration();
    }

    protected virtual void HandleEvent(IRoomEvent roomEvent)
    {
        switch (roomEvent)
        {
            case RoomEvents.GameFinished ranking:
                Rankings = ranking.Rankings;
                break;
            case RoomEvents.NextQuestion question:
                CurrentQuestion = question.Info;
                TimerConfiguration.ResetAndStart(question.Info.Question.TimeLimit);
                OnNextQuestion();
                break;
            case RoomEvents.RoundFinished:
                TimerConfiguration.Stop();
                CurrentQuestion = null;
                break;
        }
    }

    protected virtual void OnNextQuestion()
    {
    }

    protected virtual async Task AfterConnectedConfiguration()
    {
        var currentState = await RoomService.GetCurrentState(RoomId);
        if (currentState == RoomStatus.DisplayingQuestion)
        {
            CurrentQuestion = await RoomService.GetCurrentQuestion(RoomId);
            TimerConfiguration.ResetAndStart(CurrentQuestion.Question.TimeLimit);
            OnNextQuestion();
        }
    }

    private async Task RunDataPump(ChannelReader<IRoomEvent> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var roomEvent in reader.ReadAllAsync(cancellationToken))
            {
                HandleEvent(roomEvent);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _localRoomActor?.Tell(new UnsubscribeFromRoom(RoomId, _subscriptionId));
        TimerConfiguration.Dispose();
        GC.SuppressFinalize(this);
    }
}
