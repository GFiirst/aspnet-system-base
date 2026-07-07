using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly ILogger<AuthService> _logger;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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
        
        var userId = httpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var userEmail = httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

        _logger.LogInformation(
        "Audit UserId={UserId} UserEmail={UserEmail}",
        userId,
        userEmail);

        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userEmail))
        {
            userEmail = "Anonymous";
        }

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
                UserEmail = userEmail,
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