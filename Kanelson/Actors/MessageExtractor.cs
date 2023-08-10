using Akka.Cluster.Sharding;

namespace Kanelson.Actors;

#region ExtractorClass

public sealed class MessageExtractor : HashCodeMessageExtractor
{
    public MessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string? EntityId(object message)
        => message switch
        {
            ShardRegion.StartEntity start => start.EntityId,
            ShardingEnvelope e => e.EntityId,
            _ => null
        };

    public override object EntityMessage(object message)
        => message switch
        {
            ShardingEnvelope e => e.Message,
            _ => message
        };
}
#endregion