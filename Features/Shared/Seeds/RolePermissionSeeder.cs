using Microsoft.EntityFrameworkCore;

public static class RolePermissionSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var policyRolePermissions = new HashSet<(Guid RoleId, Guid PermissionId)>();
        
        foreach (var (role, policyKeys) in Policies.RolePermissions)
        {
            var roleEntity = await context.Roles
                .FirstOrDefaultAsync(r => r.Roles == role);
            
            if (roleEntity == null) continue;
            
            foreach (var policyKey in policyKeys)
            {
                var (subject, action) = Policies.AllPolicies[policyKey];
                
                var permission = await context.Permissions
                    .FirstOrDefaultAsync(p => p.Subject == subject && p.Action == action);
                
                if (permission == null) continue;
                
                policyRolePermissions.Add((roleEntity.Id, permission.Id));
            }
        }
        
        var existingRolePermissions = await context.RolesPermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        
        var existingRolePermissionsTuples = existingRolePermissions
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();
        
        var newRolePermissions = policyRolePermissions.Except(existingRolePermissionsTuples).ToList();
        foreach (var (roleId, permissionId) in newRolePermissions)
        {
            context.RolesPermissions.Add(new RolesPermissions
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.UtcNow
            });
        }
        
        var rolePermissionsToRemove = existingRolePermissionsTuples.Except(policyRolePermissions).ToList();
        foreach (var (roleId, permissionId) in rolePermissionsToRemove)
        {
            var rolePermission = await context.RolesPermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            
            if (rolePermission != null)
            {
                context.RolesPermissions.Remove(rolePermission);
            }
        }
        
        await context.SaveChangesAsync();
    }
}