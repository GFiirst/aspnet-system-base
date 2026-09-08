using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

public static class ApplicationExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        if (app.Configuration.GetValue<bool>("APPLY_MIGRATIONS_ON_STARTUP"))
        {
            Log.Information("Iniciando aplicação das migrations do banco de dados");
            await context.Database.MigrateAsync();
            Log.Information("Migrations aplicadas com sucesso");
        }

        var roleStopwatch = Stopwatch.StartNew();
        await RoleSeeder.SeedAsync(context);
        roleStopwatch.Stop();
        Log.Information(
            "RoleSeeder concluído em {ElapsedMilliseconds}ms",
            roleStopwatch.ElapsedMilliseconds
        );

        var permissionStopwatch = Stopwatch.StartNew();
        await PermissionSeeder.SeedAsync(context);
        permissionStopwatch.Stop();
        Log.Information(
            "PermissionSeeder concluído em {ElapsedMilliseconds}ms",
            permissionStopwatch.ElapsedMilliseconds
        );

        var rolePermissionStopwatch = Stopwatch.StartNew();
        await RolePermissionSeeder.SeedAsync(context);
        rolePermissionStopwatch.Stop();
        Log.Information(
            "RolePermissionSeeder concluído em {ElapsedMilliseconds}ms",
            rolePermissionStopwatch.ElapsedMilliseconds
        );

        var userAdminStopwatch = Stopwatch.StartNew();
        await UserAdminSeeder.SeedAsync(context, encryptionService);
        userAdminStopwatch.Stop();
        Log.Information(
            "UserAdminSeeder concluído em {ElapsedMilliseconds}ms",
            userAdminStopwatch.ElapsedMilliseconds
        );
    }
}
