using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder
    )
    {
        builder.ToTable("refresh_token");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(255);
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Device)
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(600);

        builder.Property(x => x.Ip)
            .HasMaxLength(45);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}