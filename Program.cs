using DotNetEnv;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
    
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        if (allowedOrigins.Length > 0 && !string.IsNullOrEmpty(allowedOrigins[0]))
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.Configure<FileUploadOptions>(options =>
    {
        options.AllowedExtensions = [
            ".pdf",
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            ".doc", ".docx",
            ".xls", ".xlsx",
            ".txt"
        ];

        options.AllowedMimeTypes = [
            "application/pdf",
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain"
        ];

        options.MaxSize = 10 * 1024 * 1024;

        options.UploadPath = builder.Configuration["FILE_PATH"] ?? "uploads";
    });

builder.Services.Configure<EmailSettings>(options =>
    {
        options.MailUser = builder.Configuration["MAIL_USER"] ?? "";
        options.MailPass = builder.Configuration["MAIL_PASS"] ?? "";
        options.FrontendUrl = builder.Configuration["FRONTEND_URL"] ?? "";
    });



var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Information()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 15, outputTemplate: outputTemplate)
    .WriteTo.Console(outputTemplate: outputTemplate)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddEncryptionService(builder.Configuration);

builder.Services.AddApplicationServices();

builder.Services.AddApiValidation();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(Policies.ConfigurePolicies);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter();

var app = builder.Build();

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

app.Run();