using System.Net;
using Microsoft.EntityFrameworkCore;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class LogoutTests : IntegrationTestBase
{
    public LogoutTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Logout_deve_revogar_refresh_e_apagar_cookies()
    {
        await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();

        var response = await Client.PostAsync("/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, cookie => cookie.StartsWith("access_token=;"));
        Assert.Contains(cookies, cookie => cookie.StartsWith("refresh_token=;"));

        var status = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.RefreshTokens
                .Select(token => token.Status)
                .SingleAsync());
        Assert.Equal(TokenStatusEnum.revoked, status);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Client.PostAsync("/auth/refresh", content: null)).StatusCode);
    }
}
