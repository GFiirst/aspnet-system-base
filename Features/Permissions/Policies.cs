using Microsoft.AspNetCore.Authorization;

public static class Policies
{
    public const string UserRead = "user.read";
    public const string UserCreate = "user.create";
    public const string UserUpdate = "user.update";
    public const string UserDelete = "user.delete";
    public const string UserManage = "user.manage";
    
    public static readonly Dictionary<string, (string Subject, PermissionAction Action)> AllPolicies = new()
    {
        [UserRead] = ("user", PermissionAction.read),
        [UserCreate] = ("user", PermissionAction.create),
        [UserUpdate] = ("user", PermissionAction.update),
        [UserDelete] = ("user", PermissionAction.delete),
        [UserManage] = ("user", PermissionAction.manage),
    };
    
    public static readonly Dictionary<RolesEnum, string[]> RolePermissions = new()
    {
        [RolesEnum.admin] = new[] { UserRead, UserCreate, UserUpdate, UserDelete, UserManage },
        [RolesEnum.user] = new[] { UserRead },
    };
    
    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        foreach (var (policyKey, (subject, action)) in AllPolicies)
        {
            options.AddPolicy(policyKey, policy => 
                policy.AddRequirements(new PermissionRequirement(subject, action)));
        }
    }
}