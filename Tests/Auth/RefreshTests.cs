using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class RefreshTests : IntegrationTestBase
{
    public RefreshTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Refresh_valido_deve_renovar_o_cookie_de_acesso()
    {
        await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();

        var response = await Client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("access_token=", StringComparison.Ordinal));
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.GetAsync("/auth/validate")).StatusCode);
    }

    [Fact]
    public async Task Refresh_sem_cookie_deve_retornar_nao_autorizado()
    {
        var response = await Client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_adulterado_deve_retornar_nao_autorizado()
    {
        await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        using var rawClient = Factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });
        rawClient.DefaultRequestHeaders.Add(
            "Cookie",
            $"refresh_token={AuthTestHelper.TamperToken(
                AuthTestHelper.GetCookieValue(loginResponse, "refresh_token"))}");

        var response = await rawClient.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_revogado_deve_retornar_nao_autorizado()
    {
        await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();
        await SetStoredTokenAsync(token => token.Status = TokenStatusEnum.revoked);

        var response = await Client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_expirado_no_banco_deve_retornar_nao_autorizado()
    {
        await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();
        await SetStoredTokenAsync(token =>
            token.ExpiredAt = DateTime.UtcNow.AddMinutes(-1));

        var response = await Client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task SetStoredTokenAsync(Action<RefreshToken> update) =>
        AuthTestHelper.ExecuteDbAsync(Factory, async context =>
        {
            var token = await context.RefreshTokens.SingleAsync();
            update(token);
            await context.SaveChangesAsync();
        });
}
