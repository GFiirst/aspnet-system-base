using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AspnetSystemBase.Tests.Infrastructure;

public static class AuthTestHelper
{
    public const string DefaultEmail = "admin@system-base.test";
    public const string DefaultPassword = "senha-valida";

    public static async Task<User> CreateUserAsync(
        ApiFactory factory,
        string email = DefaultEmail,
        string password = DefaultPassword,
        RolesEnum role = RolesEnum.admin)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryption = scope.ServiceProvider
            .GetRequiredService<IEncryptionService>();
        var persistedRole = await context.Roles.SingleAsync(item =>
            item.Roles == role);

        var normalizedEmail = email.ToLowerInvariant();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Usuário de Teste",
            EmailEncrypted = encryption.Encrypt(normalizedEmail),
            EmailHash = encryption.ComputeHash(normalizedEmail),
            Password = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            UserRoles =
            [
                new UserRole
                {
                    RoleId = persistedRole.Id,
                    CreatedAt = DateTime.UtcNow
                }
            ]
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<(HttpResponseMessage Response, ResponseLoginDto Body)>
        LoginAsync(
            HttpClient client,
            string email = DefaultEmail,
            string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginDto { Email = email, Password = password });
        var body = await response.Content.ReadFromJsonAsync<ResponseLoginDto>();

        return (response, Assert.IsType<ResponseLoginDto>(body));
    }

    public static async Task ExecuteDbAsync(
        ApiFactory factory,
        Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
    }

    public static async Task<T> ExecuteDbAsync<T>(
        ApiFactory factory,
        Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    public static async Task<string> CreatePasswordResetTokenAsync(
        ApiFactory factory,
        Guid userId,
        TimeSpan? lifetime = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryption = scope.ServiceProvider
            .GetRequiredService<IEncryptionService>();
        var jwt = scope.ServiceProvider
            .GetRequiredService<IOptions<JwtSettings>>()
            .Value;
        var user = await context.Users.SingleAsync(item => item.Id == userId);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("purpose", "password-reset"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(
                    "password-version",
                    encryption.ComputeHash(user.Password))
            ],
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.ResetPasswordKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string TamperToken(string token)
    {
        var parts = token.Split('.');
        var signature = parts[2];
        parts[2] = (signature[0] == 'A' ? 'B' : 'A') + signature[1..];
        return string.Join('.', parts);
    }

    public static string HashToken(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string GetCookieValue(
        HttpResponseMessage response,
        string cookieName)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(
                $"{cookieName}=",
                StringComparison.Ordinal));

        return cookie.Split(';', 2)[0][(cookieName.Length + 1)..];
    }
}
