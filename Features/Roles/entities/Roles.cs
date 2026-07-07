public class Role
{
    public Guid Id { get; set; }
    
    public RolesEnum Roles { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RolesPermissions> RolesPermissions { get; set; } = [];
}