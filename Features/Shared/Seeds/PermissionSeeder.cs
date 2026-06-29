using Microsoft.EntityFrameworkCore;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var policyPermissions = Policies.AllPolicies.Values
            .Select(p => new { p.Subject, p.Action })
            .ToHashSet();
        
        var existingPermissions = await context.Permissions
            .Select(p => new { p.Subject, p.Action })
            .ToListAsync();
        
        var newPermissions = policyPermissions.Except(existingPermissions).ToList();
        foreach (var perm in newPermissions)
        {
            context.Permissions.Add(new Permissions
            {
                Subject = perm.Subject,
                Action = perm.Action
            });
        }
        
        var permissionsToRemove = existingPermissions.Except(policyPermissions).ToList();
        foreach (var perm in permissionsToRemove)
        {
            var permission = await context.Permissions
                .FirstOrDefaultAsync(p => p.Subject == perm.Subject && p.Action == perm.Action);
            
            if (permission != null)
            {
                context.Permissions.Remove(permission);
            }
        }
        
        await context.SaveChangesAsync();
    }
}