using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

public class AuditInterceptor : SaveChangesInterceptor
{
    private const string RedactedValue = "[REDACTED]";
    private const string SecretChangedValue = "[SECRET_CHANGED]";

    private static readonly HashSet<string> SecretPropertyNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "TokenHash",
        "Auth",
        "P256dh"
    };

    private static readonly HashSet<string> SensitivePropertyNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Endpoint",
        "UserAgent"
    };

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly ILogger<AuditInterceptor> _logger;
    private readonly IEncryptionService _encryptionService;

    public AuditInterceptor(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditInterceptor> logger,
        IEncryptionService encryptionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _encryptionService = encryptionService;
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

        string? userId = null;
        string? ipAddress = null;

        if (httpContext != null)
        {
            userId = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        var entries = context.ChangeTracker.Entries()
        .Where(e =>
            e.Entity is not AuditLog &&
            (e.State == EntityState.Added ||
            e.State == EntityState.Modified ||
            e.State == EntityState.Deleted))
        .ToList();


        foreach (var entry in entries)
        {
            var auditUserId = Guid.TryParse(userId, out var authenticatedUserId)
                ? authenticatedUserId
                : GetEntityUserId(entry);

            var auditLog = new AuditLog
            {
                UserId = auditUserId,
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
                IpAddressEncrypted = ipAddress is null ? null : _encryptionService.Encrypt(ipAddress),
                IpAddressHash = ipAddress is null ? null : _encryptionService.ComputeHash(ipAddress)
            };

            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                
                foreach (var property in entry.Properties)
                {
                    if (property.IsModified)
                    {
                        oldValues[property.Metadata.Name] = GetAuditValue(
                            property.Metadata.Name,
                            property.OriginalValue);
                        newValues[property.Metadata.Name] = GetAuditValue(
                            property.Metadata.Name,
                            property.CurrentValue);
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
                    newValues[property.Metadata.Name] = GetAuditValue(
                        property.Metadata.Name,
                        property.CurrentValue);
                }
                auditLog.NewValues = JsonSerializer.Serialize(newValues);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var oldValues = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    oldValues[property.Metadata.Name] = GetAuditValue(
                        property.Metadata.Name,
                        property.OriginalValue);
                }
                auditLog.OldValues = JsonSerializer.Serialize(oldValues);
            }

            context.Set<AuditLog>().Add(auditLog);
        }
    }

    private static object? GetAuditValue(string propertyName, object? value)
    {
        if (SecretPropertyNames.Contains(propertyName))
        {
            return SecretChangedValue;
        }

        if (SensitivePropertyNames.Contains(propertyName)
            || propertyName.EndsWith("Encrypted", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Hash", StringComparison.OrdinalIgnoreCase))
        {
            return RedactedValue;
        }

        return value;
    }

    private static Guid? GetEntityUserId(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var userId = entry.Properties
            .FirstOrDefault(property => property.Metadata.Name == "UserId")
            ?.CurrentValue;

        return userId switch
        {
            Guid value when value != Guid.Empty => value,
            string value when Guid.TryParse(value, out var parsed) => parsed,
            _ => null
        };
    }
}
