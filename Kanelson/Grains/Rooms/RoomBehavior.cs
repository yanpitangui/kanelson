using Kanelson.Contracts.Models;
using Stateless;

namespace Kanelson.Grains.Rooms;

public static class RoomBehavior
{
    public static RoomStateMachine GetDefaultStateMachine()
    {
        var stateMachine = new RoomStateMachine();
        stateMachine.Configure(RoomStatus.Created)
            .Permit(RoomTrigger.Start, RoomStatus.Started);

        stateMachine.Configure(RoomStatus.Started)
            .Permit(RoomTrigger.DisplayQuestion, RoomStatus.DisplayingQuestion)
            .Permit(RoomTrigger.Abandon, RoomStatus.Abandoned);

        stateMachine.Configure(RoomStatus.DisplayingQuestion)
            .SubstateOf(RoomStatus.Started)
#if DEBUG
            .Permit(RoomTrigger.Start, RoomStatus.Started)
#endif
            .Permit(RoomTrigger.WaitForNextQuestion, RoomStatus.AwaitingForNextQuestion);

        stateMachine.Configure(RoomStatus.AwaitingForNextQuestion)
            .SubstateOf(RoomStatus.Started)
            
#if DEBUG
            .Permit(RoomTrigger.Start, RoomStatus.Started)
#endif
            .Permit(RoomTrigger.DisplayQuestion, RoomStatus.DisplayingQuestion)
            .Permit(RoomTrigger.Finish, RoomStatus.Finished)
            .Permit(RoomTrigger.Abandon, RoomStatus.Abandoned);

        return stateMachine;
    }
}

[GenerateSerializer]
public class RoomStateMachine : StateMachine<RoomStatus, RoomTrigger>
{
    public RoomStateMachine() : base(RoomStatus.Created)
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