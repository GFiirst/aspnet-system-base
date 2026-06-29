using Microsoft.EntityFrameworkCore;

public static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var enumRoles = Enum.GetValues<RolesEnum>().ToHashSet();
        
        var existingRoles = await context.Roles
            .Select(r => r.Roles)
            .ToListAsync();
        
        var newRoles = enumRoles.Except(existingRoles).ToList();
        foreach (var roleEnum in newRoles)
        {
            context.Roles.Add(new Role
            {
                Roles = roleEnum
            });
        }
        
        var rolesToRemove = existingRoles.Except(enumRoles).ToList();
        foreach (var roleEnum in rolesToRemove)
        {
            var role = await context.Roles
                .FirstOrDefaultAsync(r => r.Roles == roleEnum);
            
            if (role != null)
            {
                context.Roles.Remove(role);
            }
        }
        
        await context.SaveChangesAsync();
    }
}