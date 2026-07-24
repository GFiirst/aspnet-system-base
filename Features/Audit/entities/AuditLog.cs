public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public required string EntityName { get; set; }
    
    public required string EntityId { get; set; }
    
    public AuditAction Action { get; set; }
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? IpAddress { get; set; }
}