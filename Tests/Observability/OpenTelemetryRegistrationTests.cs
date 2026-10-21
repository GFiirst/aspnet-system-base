using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace AspnetSystemBase.Tests.Unit.Observability;

public sealed class OpenTelemetryRegistrationTests
{
    [Fact]
    public void Desabilitado_nao_deve_registrar_provedores()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(enabled: false);

        services.AddApplicationOpenTelemetry(
            configuration,
            new TestHostEnvironment());

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(TracerProvider));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(MeterProvider));
    }

    [Fact]
    public void Habilitado_deve_registrar_traces_e_metricas()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(enabled: true);

        services.AddApplicationOpenTelemetry(
            configuration,
            new TestHostEnvironment());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(TracerProvider));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(MeterProvider));
    }

    [Fact]
    public void Logs_habilitados_devem_rejeitar_protocolo_otlp_invalido()
    {
        var configuration = CreateConfiguration(
            enabled: true,
            protocol: "invalido");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new LoggerConfiguration().WriteToApplicationOpenTelemetry(
                configuration,
                new TestHostEnvironment()));

        Assert.Contains("Protocolo OTLP inválido", exception.Message);
    }

    private static IConfiguration CreateConfiguration(
        bool enabled,
        string? protocol = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:Enabled"] = enabled.ToString(),
                ["OpenTelemetry:ServiceName"] = "aspnet-system-base-tests",
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = protocol
            })
            .Build();

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } =
            "aspnet-system-base-tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
