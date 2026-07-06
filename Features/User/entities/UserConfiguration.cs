using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder
    ){
        builder.ConfigureBase();

        builder.ToTable("user");

        builder.Property(x => x.Name)
        .HasColumnName("name")
        .HasMaxLength(100);

        builder.HasIndex(x => x.Email)
        .IsUnique()
        .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.Email)
        .HasColumnName("email")
        .HasMaxLength(255);

        builder.Property(x => x.Password)
        .HasColumnName("password")
        .HasMaxLength(255);
    }
}