using Akka.Persistence;

namespace Kanelson.Grains.Rooms;

public class RoomActor : ReceivePersistentActor
{
    public override string PersistenceId { get; }

    public RoomActor(string roomIdentifier)
    {
        PersistenceId = roomIdentifier;
    }
}