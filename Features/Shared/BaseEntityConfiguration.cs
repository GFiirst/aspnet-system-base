using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public abstract class BaseEntityConfiguration : IEntityTypeConfiguration<BaseEntity>
{
    public void Configure(
        EntityTypeBuilder<BaseEntity> builder
    )
    {   
        builder.Property(x => x.Id)
        .HasColumnName("id");

        builder.Property(x => x.CreatedAt)
        .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
        .HasColumnName("updated_at");

        builder.Property(x => x.DeletedAt)
        .HasColumnName("deleted_at_at");
    }
}