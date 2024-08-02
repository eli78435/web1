using System.Diagnostics.Metrics;

namespace web1.Observability.Metrics;

public static class DiagnosticConfig
{
    public const string ServiceName = "web1";
    public static Meter Meter = new(ServiceName);
    public static Counter<int> GetRequestCounter = Meter.CreateCounter<int>("get.count");
}