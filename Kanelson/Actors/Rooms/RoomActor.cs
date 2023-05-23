using Akka.Actor;
using Akka.Persistence;
using Kanelson.Contracts.Models;
using Kanelson.Hubs;
using Kanelson.Services;
using Microsoft.AspNetCore.SignalR;

namespace Kanelson.Actors.Rooms;

public class RoomActor : ReceivePersistentActor, IHasSnapshotInterval
{

    public override string PersistenceId { get; }

    private readonly RoomState _state;

    public RoomActor(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        PersistenceId = $"room-{roomIdentifier}";


        _state = new RoomState();



        Command<SetBase>(o =>
        {
            _state.OwnerId = o.OwnerId;
            _state.Template = o.Template;
            _state.Name = o.RoomName;
            _state.MaxQuestionIdx = Math.Clamp(_state.Template.Questions.Count - 1, 0, 100);
            _state.CurrentQuestionIdx = 0;
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

        CommandAsync<UpdateCurrentUsers>(async o =>
        { 
            var equal = o.Users.SetEquals(_state.CurrentUsers);
             _state.CurrentUsers = o.Users;
             if (!equal)
             {
                 await hubContext.Clients.Group(roomIdentifier.ToString()).SendAsync("CurrentUsersUpdated", o.Users);
                 await hubContext.Clients.User(_state.OwnerId).SendAsync("CurrentUsersUpdated", o.Users);
             }
        });

        Command<GetCurrentQuestion>(o => { });

        Command<Start>(o => { });

        Command<NextQuestion>(o => { });
        
        Command<GetOwner>(o => { });

        Command<SendUserAnswer>(o => { });

        Command<GetCurrentUsers>(_ =>
        {
            Sender.Tell(_state.CurrentUsers);
        });
        
                
        Command<ShutdownCommand>(_ =>
        {
            DeleteMessages(Int64.MaxValue);
            DeleteSnapshots(SnapshotSelectionCriteria.Latest);
            Context.Stop(Self);
        });
        

    }
    
    
    public static Props Props(long roomIdentifier, IHubContext<RoomHub> hubContext, IUserService userService)
    {
        return Akka.Actor.Props.Create(() => new RoomActor(roomIdentifier, hubContext, userService));
    }


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