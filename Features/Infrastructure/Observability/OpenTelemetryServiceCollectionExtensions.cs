using System.Reflection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection("OpenTelemetry");
        if (!section.GetValue<bool>("Enabled"))
            return services;

        var serviceName = section["ServiceName"];
        if (string.IsNullOrWhiteSpace(serviceName))
            serviceName = environment.ApplicationName;

        var serviceVersion = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>(
                        "deployment.environment.name",
                        environment.EnvironmentName)
                ]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}
