using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder
    )
    {   
        builder.ConfigureBase();

        builder.ToTable("refresh_token");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiredAt)
            .HasColumnName("expired_at");
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Device)
            .HasColumnName("device")
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(600);

        builder.Property(x => x.Ip)
            .HasColumnName("ip")
            .HasMaxLength(45);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");
    }
}