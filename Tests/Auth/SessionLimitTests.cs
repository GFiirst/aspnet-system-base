using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class SessionLimitTests : IntegrationTestBase
{
    public SessionLimitTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Sexto_login_deve_revogar_a_sessao_ativa_mais_antiga()
    {
        var user = await AuthTestHelper.CreateUserAsync(Factory);
        var tokenIds = Enumerable.Range(0, 5)
            .Select(_ => Guid.NewGuid())
            .ToList();

        await AuthTestHelper.ExecuteDbAsync(Factory, async context =>
        {
            var baseTime = DateTime.UtcNow.AddHours(-1);

            for (var index = 0; index < tokenIds.Count; index++)
            {
                context.RefreshTokens.Add(new RefreshToken
                {
                    Id = tokenIds[index],
                    UserId = user.Id,
                    TokenHash = Convert.ToHexString(SHA256.HashData(
                        Encoding.UTF8.GetBytes($"token-{index}"))),
                    Status = TokenStatusEnum.active,
                    ExpiredAt = DateTime.UtcNow.AddDays(1)
                });
            }

            await context.SaveChangesAsync();

            var tokens = await context.RefreshTokens
                .OrderBy(token => token.Id)
                .ToListAsync();
            foreach (var token in tokens)
            {
                var index = tokenIds.IndexOf(token.Id);
                token.CreatedAt = baseTime.AddMinutes(index);
            }

            await context.SaveChangesAsync();
        });

        var (response, _) = await AuthTestHelper.LoginAsync(Client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokensAfterLogin = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.RefreshTokens.AsNoTracking().ToListAsync());
        Assert.Equal(
            TokenStatusEnum.revoked,
            tokensAfterLogin.Single(token => token.Id == tokenIds[0]).Status);
        Assert.Equal(
            5,
            tokensAfterLogin.Count(token => token.Status == TokenStatusEnum.active));
    }
}
