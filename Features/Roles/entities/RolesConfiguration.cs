using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RolesConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(
        EntityTypeBuilder<Role> builder
    )
    {
        builder.ToTable("roles");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Roles)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}