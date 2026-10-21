using System.Reflection;
using OpenTelemetry;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

public static class SerilogOpenTelemetryExtensions
{
    public static LoggerConfiguration WriteToApplicationOpenTelemetry(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection("OpenTelemetry");
        if (!section.GetValue<bool>("Enabled"))
            return loggerConfiguration;

        var serviceName = section["ServiceName"];
        if (string.IsNullOrWhiteSpace(serviceName))
            serviceName = environment.ApplicationName;

        var serviceVersion = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        return loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
                ["service.version"] = serviceVersion,
                ["service.instance.id"] = Environment.MachineName,
                ["deployment.environment.name"] = environment.EnvironmentName
            };

            options.OnBeginSuppressInstrumentation =
                SuppressInstrumentationScope.Begin;

            var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(endpoint))
                options.Endpoint = endpoint;

            var protocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
            if (!string.IsNullOrWhiteSpace(protocol))
            {
                options.Protocol = protocol.ToLowerInvariant() switch
                {
                    "grpc" => OtlpProtocol.Grpc,
                    "http/protobuf" => OtlpProtocol.HttpProtobuf,
                    _ => throw new InvalidOperationException(
                        $"Protocolo OTLP inválido: '{protocol}'. "
                        + "Use 'grpc' ou 'http/protobuf'.")
                };
            }
        });
    }
}
