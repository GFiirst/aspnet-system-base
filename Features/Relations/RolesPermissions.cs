public class RolesPermissions
{
    public Guid PermissionId { get; set; }
    public Permissions Permission { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}