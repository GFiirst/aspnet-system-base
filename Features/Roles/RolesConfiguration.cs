using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RolesConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(
        EntityTypeBuilder<Role> builder
    )
    {
        builder.ConfigureBase();

        builder.ToTable("roles");

        builder.Property(x => x.Roles)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}