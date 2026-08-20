using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddCors(options =>
        {
            var allowedOrigins = configuration["Cors:AllowedOrigins"]?.Split(',') ?? [];

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

        services.AddApplicationOptions(configuration);
        services.AddDatabase(configuration);
        services.AddEncryptionService(configuration);
        services.AddApplicationServices();
        services.AddApiValidation();
        services.AddJwtAuthentication(configuration);
        services.AddHttpContextAccessor();
        services.AddAuthorization(Policies.ConfigurePolicies);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
            options.EnableAnnotations();
        });
        services.AddRateLimiter();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileUploadOptions>(options =>
        {
            options.AllowedExtensions =
            [
                ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp",
                ".doc", ".docx", ".xls", ".xlsx", ".txt"
            ];
            options.AllowedMimeTypes =
            [
                "application/pdf", "image/jpeg", "image/png", "image/gif", "image/webp",
                "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "text/plain"
            ];
            options.MaxSize = 10 * 1024 * 1024;
            options.UploadPath = configuration["FILE_PATH"] ?? "uploads";
        });

        services.Configure<EmailSettings>(options =>
        {
            options.MailUser = configuration["MAIL_USER"] ?? "";
            options.MailPass = configuration["MAIL_PASS"] ?? "";
            options.FrontendUrl = configuration["FRONTEND_URL"] ?? "";
        });

        return services;
    }

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileValidator, FileValidator>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddScoped<AuditInterceptor>();

        return services;
    }

    public static IServiceCollection AddEncryptionService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var encryptionKey = configuration["Encryption:Key"];
        
        if (string.IsNullOrEmpty(encryptionKey))
        {
            encryptionKey = "DefaultKeyForDesignTime32Chars!!";
        }

        services.AddSingleton<IEncryptionService>(new AesEncryptionService(encryptionKey));

        return services;
    }

    public static IServiceCollection AddApiValidation(
        this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .SelectMany(x => x.Value!.Errors)
                .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? e.ErrorMessage
                    : e.Exception?.Message)
                .ToList();

            return new BadRequestObjectResult(errors);
        };
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection("Jwt"));

        var jwt = configuration
            .GetSection("Jwt")
            .Get<JwtSettings>()!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,

                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.AccessKey)
                )
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["access_token"];

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build()
            );

        return services;
    }

    public static IServiceCollection AddRateLimiter(
        this IServiceCollection services
    )
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("Default", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
