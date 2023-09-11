using Akka.Cluster.Sharding;
using Kanelson.Actors.Rooms;

namespace Kanelson.Actors;

public class UserMessageExtractor : HashCodeMessageExtractor
{
    public UserMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string? EntityId(object message)
    {
        return message switch
        {

            IWithUserId e => e.UserId,
            _ => null
        };
    }
}