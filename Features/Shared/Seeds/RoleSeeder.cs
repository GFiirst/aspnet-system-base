using Microsoft.EntityFrameworkCore;

public static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        foreach (RolesEnum roleEnum in Enum.GetValues<RolesEnum>())
        {
            var exists = await context.Roles
                .AnyAsync(x => x.Roles == roleEnum);

            if (!exists)
            {
                context.Roles.Add(new Role
                {
                    Roles = roleEnum
                });
            }
        }

        await context.SaveChangesAsync();
    }
}