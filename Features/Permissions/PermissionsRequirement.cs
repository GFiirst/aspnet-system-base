using Microsoft.AspNetCore.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Subject { get; }
    public PermissionAction Action { get; }
    
    public PermissionRequirement(string subject, PermissionAction action)
    {
        Subject = subject;
        Action = action;
    }
}