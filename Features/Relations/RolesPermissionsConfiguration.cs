using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RolesPermissionsConfiguration : IEntityTypeConfiguration<RolesPermissions>
{
    public void Configure(
        EntityTypeBuilder<RolesPermissions> builder
    )
    {
        builder.ToTable("role_permission");

        builder.HasKey(x => new { x.PermissionId, x.RoleId });

        builder.Property(x => x.PermissionId)
            .HasColumnName("permission_id");

        builder.Property(x => x.RoleId)
            .HasColumnName("role_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}