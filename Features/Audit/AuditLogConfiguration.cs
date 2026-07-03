using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        
        builder.Property(x => x.Id)
        .HasColumnName("id");
        
        builder.Property(x => x.UserId)
        .HasColumnName("user_id");
        
        builder.Property(x => x.UserName)
        .HasColumnName("user_name");
        
        builder.Property(x => x.EntityName)
        .HasColumnName("entity_name");
        
        builder.Property(x => x.EntityId)
        .HasColumnName("entity_id");
        
        builder.Property(x => x.Action)
        .HasColumnName("action")
        .HasConversion<string>();
        
        builder.Property(x => x.OldValues)
        .HasColumnName("old_values");
        
        builder.Property(x => x.NewValues)
        .HasColumnName("new_values");
        
        builder.Property(x => x.Timestamp)
        .HasColumnName("timestamp");
        
        builder.Property(x => x.IpAddress)
        .HasColumnName("ip_address");
    }
}