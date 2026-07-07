using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PermissionsConfiguration : IEntityTypeConfiguration<Permissions>
{
    public void Configure(
        EntityTypeBuilder<Permissions> builder
    )
    {
        builder.ToTable("permissions");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Subject)
            .HasColumnName("subject");
    }   
}