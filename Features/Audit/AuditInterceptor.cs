using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        AuditChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AuditChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AuditChanges(DbContext? context)
    {
        if (context == null) return;

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst("sub")?.Value;
        var userName = httpContext?.User?.Identity?.Name;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added 
                     || e.State == EntityState.Modified 
                     || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                UserId = Guid.TryParse(userId, out var guid) ? guid : null,
                UserName = userName,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "",
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.create,
                    EntityState.Modified => AuditAction.update,
                    EntityState.Deleted => AuditAction.delete,
                    _ => AuditAction.read
                },
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                
                foreach (var property in entry.Properties)
                {
                    if (property.IsModified)
                    {
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        newValues[property.Metadata.Name] = property.CurrentValue;
                    }
                }
                
                auditLog.OldValues = JsonSerializer.Serialize(oldValues);
                auditLog.NewValues = JsonSerializer.Serialize(newValues);
            }
            else if (entry.State == EntityState.Added)
            {
                var newValues = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    newValues[property.Metadata.Name] = property.CurrentValue;
                }
                auditLog.NewValues = JsonSerializer.Serialize(newValues);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var oldValues = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    oldValues[property.Metadata.Name] = property.OriginalValue;
                }
                auditLog.OldValues = JsonSerializer.Serialize(oldValues);
            }

            context.Set<AuditLog>().Add(auditLog);
        }
    }
}