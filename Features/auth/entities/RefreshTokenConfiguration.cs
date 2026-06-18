using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(
        EntityTypeBuilder<RefreshToken> builder
    )
    {
        builder.ToTable("refresh_token");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}