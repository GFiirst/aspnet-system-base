using Microsoft.EntityFrameworkCore;

public static class ApplicationExtensions
{
    public static async Task SeedDatabaseAsync(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        await RoleSeeder.SeedAsync(context);
        await PermissionSeeder.SeedAsync(context);
        await RolePermissionSeeder.SeedAsync(context);
        await UserAdminSeeder.SeedAsync(context, encryptionService);
    }
}