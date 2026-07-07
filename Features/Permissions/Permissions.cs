public class Permissions
{
    public Guid Id { get; set; }
    
    public PermissionAction Action { get; set; }

    public string Subject { get; set; } = "";

    public ICollection<RolesPermissions> RolesPermissions { get; set; } = [];
}