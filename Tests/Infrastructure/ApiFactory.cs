using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace AspnetSystemBase.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string EncryptionKey =
        "0123456789abcdef0123456789abcdef";

    private const string JwtAccessKey =
        "system-base-tests-access-key-0123456789abcdef0123456789abcdef";

    private const string JwtRefreshKey =
        "system-base-tests-refresh-key-0123456789abcdef0123456789abcdef";

    private const string JwtResetPasswordKey =
        "system-base-tests-reset-key-0123456789abcdef0123456789abcdef";

    private readonly string _temporaryPath = Path.Combine(
        Path.GetTempPath(),
        $"aspnet-system-base-tests-{Guid.NewGuid():N}");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("system_base_tests")
        .WithUsername("system_base_tests")
        .WithPassword("system_base_tests_password")
        .Build();

    private Respawner? _respawner;

    public string UploadPath => Path.Combine(_temporaryPath, "uploads");

    public string LogPath => Path.Combine(_temporaryPath, "logs");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    _postgres.GetConnectionString(),
                ["Encryption:Key"] = EncryptionKey,
                ["Jwt:AccessKey"] = JwtAccessKey,
                ["Jwt:RefreshKey"] = JwtRefreshKey,
                ["Jwt:ResetPasswordKey"] = JwtResetPasswordKey,
                ["Jwt:Issuer"] = "AspnetSystemBase.Tests",
                ["Jwt:Audience"] = "AspnetSystemBase.Tests.Users",
                ["Jwt:AccessExpirationMinutes"] = "15",
                ["Jwt:ResetPasswordExpirationMinutes"] = "15",
                ["Jwt:RefreshExpirationDays"] = "30",
                ["CookieSettings:Secure"] = "false",
                ["Cors:AllowedOrigins"] = "http://localhost",
                ["FILE_PATH"] = UploadPath,
                ["LOG_PATH"] = LogPath,
                ["FRONTEND_URL"] = "http://localhost",
                ["APPLY_MIGRATIONS_ON_STARTUP"] = "true"
            };

            configuration.AddInMemoryCollection(testSettings);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(
            _postgres.GetConnectionString());
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore =
                [
                    new Table("__EFMigrationsHistory"),
                    new Table("roles"),
                    new Table("permissions"),
                    new Table("role_permission")
                ]
            });

        await _respawner.ResetAsync(connection);
        ResetUploads();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        Directory.CreateDirectory(UploadPath);
        Directory.CreateDirectory(LogPath);
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        await _postgres.DisposeAsync();

        if (Directory.Exists(_temporaryPath))
            Directory.Delete(_temporaryPath, recursive: true);
    }

    private void ResetUploads()
    {
        if (Directory.Exists(UploadPath))
            Directory.Delete(UploadPath, recursive: true);

        Directory.CreateDirectory(UploadPath);
    }
}
