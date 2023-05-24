using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public class RoomActor : ReceivePersistentActor, IHasSnapshotInterval, IWithTimers
{

    public override string PersistenceId { get; }

    private RoomState _state;

    public RoomActor(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        PersistenceId = $"room-{roomIdentifier}";
        
        _state = new RoomState();
        
        Recover<SetBase>(HandleSetBase);
        
        Command<SetBase>(o =>
        {
            Persist(o, HandleSetBase);
        });
        
        Command<GetCurrentState>(_ =>
        {
            Sender.Tell(_state.CurrentState);
        });
        
        CommandAsync<GetSummary>(async _ =>
        {
            var ownerInfo = await userService.GetUserInfo(_state.OwnerId); 
            var summary = new RoomSummary(roomIdentifier,
                _state.Name,
                ownerInfo,
                _state.CurrentState);
            Sender.Tell(summary);
        });

        Recover<UpdateCurrentUsers>(o =>
        {

            var equal = o.Users.SetEquals(_state.CurrentUsers);
            HandleUpdateUsers(o);
            if (!equal)
            {
                Self.Tell(new SendSignalrGroupMessage(roomIdentifier.ToString(), SignalRMessages.CurrentUsersUpdated, o.Users));
                Self.Tell(new SendSignalrUserMessage(_state.OwnerId, SignalRMessages.CurrentUsersUpdated, o.Users));
            }
        });

        CommandAsync<SendSignalrGroupMessage>(async o =>
        {
            await hubContext.Clients.Group(o.GroupId).SendAsync(o.MessageName, o.Data);
        });

        CommandAsync<SendSignalrUserMessage>(async o =>
        {
            await hubContext.Clients.User(o.UserId).SendAsync(o.MessageName, o.Data);
        });


        Command<UpdateCurrentUsers>(o =>
        { 
            var equal = o.Users.SetEquals(_state.CurrentUsers);

            Persist(o, HandleUpdateUsers);
            if (!equal)
            {
                Self.Tell(new SendSignalrGroupMessage(roomIdentifier.ToString(), SignalRMessages.CurrentUsersUpdated, o.Users));
                Self.Tell(new SendSignalrUserMessage(_state.OwnerId, SignalRMessages.CurrentUsersUpdated, o.Users));
            }

        });

        Command<GetCurrentQuestion>(_ => Sender.Tell(_state.Template.Questions[_state.CurrentQuestionIdx]));

        Command<Start>(o => { });

        Command<NextQuestion>(o => { });
        
        Command<GetOwner>(o => Sender.Tell(_state.OwnerId));

        Command<SendUserAnswer>(o => { });

        Command<GetCurrentUsers>(_ =>
        {
            Sender.Tell(_state.CurrentUsers);
        });
        
                
        Command<ShutdownCommand>(_ =>
        {
            DeleteMessages(Int64.MaxValue);
            DeleteSnapshots(SnapshotSelectionCriteria.Latest);
        });
        
        Recover<SnapshotOffer>(o =>
        {
            if (o.Snapshot is RoomState state)
            {
                _state = state;
            }
        });
        
        Command<SaveSnapshotSuccess>(success => {
            // soft-delete the journal up until the sequence # at
            // which the snapshot was taken
            DeleteMessages(success.Metadata.SequenceNr); 
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });

        Command<DeleteSnapshotsSuccess>(o =>
        {
            if (o.Criteria.Equals(SnapshotSelectionCriteria.Latest))
            {
                Context.Stop(Self);
            }
        });
        Command<DeleteMessagesSuccess>(_ => { });
    }

    private record SendSignalrGroupMessage(string GroupId, string MessageName, object Data);
    
    private record SendSignalrUserMessage(string UserId, string MessageName, object Data);



    private void HandleUpdateUsers(UpdateCurrentUsers r)
    {
        _state.CurrentUsers = r.Users;
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }

    private void HandleSetBase(SetBase r)
    {
        _state.OwnerId = r.OwnerId;
        _state.Template = r.Template;
        _state.Name = r.RoomName;
        _state.MaxQuestionIdx = Math.Clamp(_state.Template.Questions.Count - 1, 0, 100);
        _state.CurrentQuestionIdx = 0;
        ((IHasSnapshotInterval) this).SaveSnapshotIfPassedInterval(_state);
    }


    public static Props Props(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        return Akka.Actor.Props.Create(() => new RoomActor(roomIdentifier, hubContext, userService));
    }


    public ITimerScheduler Timers { get; set; } = null!;
}

public record SetBase(string RoomName, string OwnerId, Template Template);

public record GetCurrentState;

public record GetSummary;


public record UpdateCurrentUsers(HashSet<UserInfo> Users);

public record GetCurrentQuestion;

public record GetCurrentUsers;


public record Start;

public record NextQuestion;


public record GetOwner;

public record SendUserAnswer(string UserId, Guid AnswerId);