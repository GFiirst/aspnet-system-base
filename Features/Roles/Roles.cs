public class Role : BaseEntity
{
    public RolesEnum Roles {get; set;}

    public ICollection<UserRole> UserRoles { get; set; } = [];
}