using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public static class ServiceCollectionExtensions
{
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