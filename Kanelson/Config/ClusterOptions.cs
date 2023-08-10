namespace Kanelson.Config;

public sealed class ClusterOptions
{
    public string? Ip { get; set; }
    public int? Port { get; set; }
    public string[]? Seeds { get; set; }
    public StartupMethod StartupMethod { get; set; } = StartupMethod.SeedNodes;
    public DiscoveryOptions Discovery { get; set; } = new ();
    public int ReadinessPort { get; set; } = 11001;
    public int PbmPort { get; set; } = 9110;
    public bool IsDocker { get; set; }
}