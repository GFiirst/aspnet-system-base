public class Permissions :BaseEntity
{
    public PermissionAction Action{get; set;}

    public string Subject {get; set;} = "";

    public ICollection<RolesPermissions> RolesPermissions { get; set; } = [];
}