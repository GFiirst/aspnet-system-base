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

        builder.Property(x => x.EmailEncrypted)
        .HasColumnName("email_encrypted")
        .HasMaxLength(256);

        builder.HasIndex(x => x.EmailHash)
        .IsUnique()
        .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.EmailHash)
        .HasColumnName("email_hash")
        .HasMaxLength(64);

        builder.Property(x => x.Password)
        .HasColumnName("password")
        .HasMaxLength(255);
    }
}