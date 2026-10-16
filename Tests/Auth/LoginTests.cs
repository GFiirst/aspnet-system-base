using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class LoginTests : IntegrationTestBase
{
    public LoginTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_valido_deve_criar_sessao_e_cookies_seguros()
    {
        var user = await AuthTestHelper.CreateUserAsync(Factory);

        var (response, body) = await AuthTestHelper.LoginAsync(Client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(user.Id, body.UserInfo.Id);
        Assert.Equal(AuthTestHelper.DefaultEmail, body.UserInfo.Email);
        Assert.Contains("admin", body.UserInfo.Roles);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);

        using var responseJson = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.False(responseJson.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(responseJson.RootElement.TryGetProperty("refreshToken", out _));

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, cookie => HasCookieSecurity(cookie, "access_token"));
        Assert.Contains(cookies, cookie => HasCookieSecurity(cookie, "refresh_token"));

        var persistedToken = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.RefreshTokens.AsNoTracking().SingleAsync());

        Assert.Equal(TokenStatusEnum.active, persistedToken.Status);
        Assert.Equal(
            AuthTestHelper.HashToken(
                AuthTestHelper.GetCookieValue(response, "refresh_token")),
            persistedToken.TokenHash);
    }

    [Fact]
    public async Task Login_deve_retornar_a_mesma_resposta_para_email_ou_senha_invalidos()
    {
        await AuthTestHelper.CreateUserAsync(Factory);

        var unknownEmail = await Client.PostAsJsonAsync(
            "/auth/login",
            new LoginDto
            {
                Email = "inexistente@system-base.test",
                Password = AuthTestHelper.DefaultPassword
            });
        var wrongPassword = await Client.PostAsJsonAsync(
            "/auth/login",
            new LoginDto
            {
                Email = AuthTestHelper.DefaultEmail,
                Password = "senha-incorreta"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(
            await unknownEmail.Content.ReadAsStringAsync(),
            await wrongPassword.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Validate_deve_exigir_cookie_de_acesso()
    {
        var response = await Client.GetAsync("/auth/validate");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Validate_deve_retornar_o_usuario_da_sessao()
    {
        var user = await AuthTestHelper.CreateUserAsync(
            Factory,
            role: RolesEnum.user);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync("/auth/validate");
        var body = await response.Content.ReadFromJsonAsync<ValidateResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(user.Id, body.Id);
        Assert.Equal(AuthTestHelper.DefaultEmail, body.Email);
        Assert.Equal(["user"], body.Roles);
    }

    [Fact]
    public async Task Validate_deve_rejeitar_usuario_removido_apos_o_login()
    {
        var user = await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();

        await AuthTestHelper.ExecuteDbAsync(Factory, async context =>
        {
            var persistedUser = await context.Users.SingleAsync(item =>
                item.Id == user.Id);
            persistedUser.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        });

        var response = await Client.GetAsync("/auth/validate");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static bool HasCookieSecurity(string cookie, string name) =>
        cookie.StartsWith($"{name}=", StringComparison.Ordinal)
        && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)
        && cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase)
        && !cookie.Contains("secure", StringComparison.OrdinalIgnoreCase);
}
