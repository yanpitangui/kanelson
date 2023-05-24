namespace Kanelson;

public record AkkaConfig
{
    public required string ClusterIp { get; init; } = "localhost";

    public required int ClusterPort { get; init; } = 7918;
    
    public bool KubernetesDiscovery { get; init; }
}