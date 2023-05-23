using Akka.Actor;
using Kanelson.Contracts.Models;

namespace Kanelson.Actors.Rooms;

public class RoomActor : ReceiveActor
{
    //public override string PersistenceId { get; }

    private readonly RoomState _state;
    
    public RoomActor(long roomIdentifier)
    {
        //PersistenceId = roomIdentifier;


        _state = new RoomState();



        Receive<GetCurrentState>(o => { });
        
        Receive<GetSummary>(o => { });

        Receive<UpdateCurrentUsers>(o => { });

        Receive<GetCurrentQuestion>(o => { });

        Receive<Start>(o => { });

        Receive<NextQuestion>(o => { });
        
        Receive<GetOwner>(o => { });

        Receive<SendUserAnswer>(o => { });

    }
    
    
    public static Props Props(long roomIdentifier)
    {
        return Akka.Actor.Props.Create(() => new RoomActor(roomIdentifier));
    }
    
    
    
    
    
}


public record GetCurrentState;

public record GetSummary;


public record UpdateCurrentUsers(HashSet<UserInfo> Users);

public record GetCurrentQuestion;


public record Start;

public record NextQuestion;


public record GetOwner;

public record SendUserAnswer(string UserId, Guid AnswerId);