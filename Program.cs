using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    Env.Load();
}

builder.Configuration.AddEnvironmentVariables();

const string outputTemplate =
    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] "
    + "[{TraceId}/{SpanId}] {SourceContext} {Message:lj}{NewLine}{Exception}";
var logPath = builder.Configuration["LOG_PATH"] ?? "logs";

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Information()
    .WriteTo.File($"{logPath}/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 15, outputTemplate: outputTemplate)
    .WriteTo.Console(outputTemplate: outputTemplate)
    .Enrich.FromLogContext();

loggerConfiguration.WriteToApplicationOpenTelemetry(
    builder.Configuration,
    builder.Environment);

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplicationOpenTelemetry(
    builder.Configuration,
    builder.Environment);
var knownProxyAddresses = builder.Configuration["ForwardedHeaders:KnownProxies"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];
var knownProxyNetworks = builder.Configuration["ForwardedHeaders:KnownNetworks"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    foreach (var address in knownProxyAddresses)
    {
        if (!IPAddress.TryParse(address, out var proxyAddress))
        {
            throw new InvalidOperationException(
                $"Endereço inválido em ForwardedHeaders:KnownProxies: '{address}'.");
        }

        options.KnownProxies.Add(proxyAddress);

        if (proxyAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            options.KnownProxies.Add(proxyAddress.MapToIPv6());
        }
    }

    foreach (var network in knownProxyNetworks)
    {
        if (!System.Net.IPNetwork.TryParse(network, out var proxyNetwork))
        {
            throw new InvalidOperationException(
                $"Rede inválida em ForwardedHeaders:KnownNetworks: '{network}'. " +
                "Use a notação CIDR, por exemplo 172.18.0.0/16.");
        }

        options.KnownIPNetworks.Add(proxyNetwork);

        if (proxyNetwork.BaseAddress.AddressFamily ==
            System.Net.Sockets.AddressFamily.InterNetwork)
        {
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(
                proxyNetwork.BaseAddress.MapToIPv6(),
                proxyNetwork.PrefixLength + 96));
        }
    }
});

var buildStopwatch = Stopwatch.StartNew();
Log.Information("Construindo aplicação...");
var app = builder.Build();
buildStopwatch.Stop();
Log.Information(
    "Aplicação construída com sucesso em {ElapsedMilliseconds}ms",
    buildStopwatch.ElapsedMilliseconds
);

var seedStopwatch = Stopwatch.StartNew();
Log.Information("Iniciando seed do banco de dados...");
await app.SeedDatabaseAsync();
seedStopwatch.Stop();
Log.Information(
    "Seed do banco de dados concluído com sucesso em {ElapsedMilliseconds}ms",
    seedStopwatch.ElapsedMilliseconds
);

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
