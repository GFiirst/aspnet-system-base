using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public static class BaseEntityMapping
{
    public static void ConfigureBase(this EntityTypeBuilder builder)
    {
        builder.Property("Id").HasColumnName("id");

        builder.Property("CreatedAt").HasColumnName("created_at");

        builder.Property("UpdatedAt").HasColumnName("updated_at");

        builder.Property("DeletedAt").HasColumnName("deleted_at");
    }
}