using Microsoft.EntityFrameworkCore;

public static class UserAdminSeeder
{
    public static async Task SeedAsync(AppDbContext context, IEncryptionService encryptionService)
    {
        var adminName = Environment.GetEnvironmentVariable("ADMIN_NAME");
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(adminName) || string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            return;
        }

        var normalizedEmail = adminEmail.ToLowerInvariant();
        var emailHash = encryptionService.ComputeHash(normalizedEmail);

        var existingAdmin = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash);

        if (existingAdmin != null)
        {
            return;
        }

        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Roles == RolesEnum.admin);

        if (adminRole == null)
        {
            return;
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        var encryptedEmail = encryptionService.Encrypt(normalizedEmail);

        var adminUser = new User
        {
            Name = adminName,
            EmailEncrypted = encryptedEmail,
            EmailHash = emailHash,
            Password = hashedPassword
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();
    }
}