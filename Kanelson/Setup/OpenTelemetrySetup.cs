using Kanelson.Tracing;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Kanelson.Setup;

public static class OpenTelemetrySetup
{
    public static IHostBuilder AddOpenTelemetrySetup(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((ctx, services) =>
        {
            var tracingOptions = ctx.Configuration.GetSection("Tracing")
                .Get<TracingOptions>();


            services
                .AddOpenTelemetry()
                .ConfigureResource(rb => rb.AddService(serviceName: OpenTelemetryExtensions.ServiceName))
                .WithTracing(telemetry =>
                {
                    telemetry
                        .AddSource(OpenTelemetryExtensions.ServiceName)
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(serviceName: OpenTelemetryExtensions.ServiceName))
                        .AddAspNetCoreInstrumentation();

                    if (tracingOptions is { Enabled: true })
                    {
                        telemetry.AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(tracingOptions.Uri);
                        });
                    }
                });
        });
    }
}