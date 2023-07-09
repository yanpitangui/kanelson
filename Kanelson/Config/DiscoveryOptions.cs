
namespace Kanelson.Config;

public sealed class DiscoveryOptions
{
    public string? ServiceName { get; set; } = null;
    public string? PortName { get; set; } = null;
    public int ManagementPort { get; set; } = 8558;
    public List<string>? ConfigEndpoints { get; set; } = null;
    public string LabelSelector { get; set; } = "cluster={0}";
}