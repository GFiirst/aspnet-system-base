using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

public static class ApplicationExtensions
{
    public static async Task SeedDatabaseAsync(
        this WebApplication app
    )
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

        var seedStopwatch = Stopwatch.StartNew();
        await RoleSeeder.SeedAsync(context);
        await PermissionSeeder.SeedAsync(context);
        await RolePermissionSeeder.SeedAsync(context);
        await UserAdminSeeder.SeedAsync(context, encryptionService);
        seedStopwatch.Stop();
        Log.Information("Seeds concluídos em {ElapsedMilliseconds}ms", seedStopwatch.ElapsedMilliseconds);
    }
}
