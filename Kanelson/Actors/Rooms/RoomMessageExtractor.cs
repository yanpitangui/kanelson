using Akka.Cluster.Sharding;

namespace Kanelson.Actors.Rooms;

public class RoomMessageExtractor : HashCodeMessageExtractor
{
    public RoomMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string? EntityId(object message)
    {
        return message switch
        {

            IWithRoomId e => e.RoomId,
            _ => null
        };
    }
}