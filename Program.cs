using DotNetEnv;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
builder.Configuration.AddEnvironmentVariables();

var isDevelopment = builder.Environment.IsDevelopment();
var outputTemplate = isDevelopment
    ? "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
    : "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}";
var logPath = builder.Configuration["LOG_PATH"] ?? "logs";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Information()
    .WriteTo.File($"{logPath}/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 15, outputTemplate: outputTemplate)
    .WriteTo.Console(outputTemplate: outputTemplate)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApiServices(builder.Configuration);

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
