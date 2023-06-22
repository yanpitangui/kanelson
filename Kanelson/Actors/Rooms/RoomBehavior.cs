using Kanelson.Models;
using Stateless;

namespace Kanelson.Actors.Rooms;

public static class RoomBehavior
{
    public static RoomStateMachine GetStateMachine(RoomStatus status)
    {
        var stateMachine = new RoomStateMachine(status);
        stateMachine.Configure(RoomStatus.Created)
            .Permit(RoomTrigger.Start, RoomStatus.Started);

        stateMachine.Configure(RoomStatus.Started)
            .Permit(RoomTrigger.DisplayQuestion, RoomStatus.DisplayingQuestion)
            .Permit(RoomTrigger.Abandon, RoomStatus.Abandoned);

        stateMachine.Configure(RoomStatus.DisplayingQuestion)
            .SubstateOf(RoomStatus.Started)
            .Permit(RoomTrigger.Finish, RoomStatus.Finished)
            .Permit(RoomTrigger.WaitForNextQuestion, RoomStatus.AwaitingForNextQuestion);

        stateMachine.Configure(RoomStatus.AwaitingForNextQuestion)
            .SubstateOf(RoomStatus.Started)
            .Permit(RoomTrigger.DisplayQuestion, RoomStatus.DisplayingQuestion)
            .Permit(RoomTrigger.Abandon, RoomStatus.Abandoned);


        return stateMachine;
    }
}

public class RoomStateMachine : StateMachine<RoomStatus, RoomTrigger>
{
    public RoomStateMachine(RoomStatus status) : base(status)
    {
        
    }
}


public enum RoomTrigger
{
    Start,
    DisplayQuestion,
    WaitForNextQuestion,
    Finish,
    Abandon
}