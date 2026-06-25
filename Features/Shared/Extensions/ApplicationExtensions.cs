using Microsoft.EntityFrameworkCore;

public static class ApplicationExtensions
{
    public static async Task SeedDatabaseAsync(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await RoleSeeder.SeedAsync(context);
    }
}