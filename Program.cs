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
var app = builder.Build();
buildStopwatch.Stop();
Log.Information("Aplicação construída em {ElapsedMilliseconds}ms", buildStopwatch.ElapsedMilliseconds);

await app.SeedDatabaseAsync();

app.UseMiddleware<GlobalErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
