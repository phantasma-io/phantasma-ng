namespace Phantasma.Node.Configuration;

public class PerformanceMetricsSettings
{
    public bool CountsEnabled { get; set; }
    public bool AveragesEnabled { get; set; }
    public int LongRunningRequestThreshold { get; set; } = 500;
}
