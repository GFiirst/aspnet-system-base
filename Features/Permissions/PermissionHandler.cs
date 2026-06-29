using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public PermissionHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return;
        
        var hasPermission = await _context.UserRoles
            .Where(ur => ur.UserId == Guid.Parse(userId))
            .SelectMany(ur => ur.Role.RolesPermissions)
            .AnyAsync(rp => rp.Permission.Subject == requirement.Subject && 
                           rp.Permission.Action == requirement.Action);
        
        if (hasPermission)
            context.Succeed(requirement);
    }
}