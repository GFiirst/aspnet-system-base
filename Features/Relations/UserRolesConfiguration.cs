using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(
        EntityTypeBuilder<UserRole> builder
    )
    {
        builder.ToTable("user_role");

         builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.RoleId)
            .HasColumnName("role_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}