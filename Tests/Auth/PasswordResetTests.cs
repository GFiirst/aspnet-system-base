using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class PasswordResetTests : IntegrationTestBase
{
    public PasswordResetTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Reset_deve_alterar_senha_revogar_sessoes_e_invalidar_token_usado()
    {
        const string newPassword = "nova-senha-valida";
        var user = await AuthTestHelper.CreateUserAsync(Factory);
        var (loginResponse, _) = await AuthTestHelper.LoginAsync(Client);
        loginResponse.EnsureSuccessStatusCode();
        var resetToken = await AuthTestHelper.CreatePasswordResetTokenAsync(
            Factory,
            user.Id);

        var response = await Client.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordDto
            {
                Token = resetToken,
                NewPassword = newPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshStatus = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.RefreshTokens
                .Select(token => token.Status)
                .SingleAsync());
        Assert.Equal(TokenStatusEnum.revoked, refreshStatus);

        using var oldPasswordClient = Factory.CreateClient();
        var oldPasswordResponse = await oldPasswordClient.PostAsJsonAsync(
            "/auth/login",
            new LoginDto
            {
                Email = AuthTestHelper.DefaultEmail,
                Password = AuthTestHelper.DefaultPassword
            });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        using var newPasswordClient = Factory.CreateClient();
        var newPasswordResponse = await newPasswordClient.PostAsJsonAsync(
            "/auth/login",
            new LoginDto
            {
                Email = AuthTestHelper.DefaultEmail,
                Password = newPassword
            });
        Assert.Equal(HttpStatusCode.OK, newPasswordResponse.StatusCode);

        var reusedTokenResponse = await Client.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordDto
            {
                Token = resetToken,
                NewPassword = "outra-senha-valida"
            });
        Assert.Equal(HttpStatusCode.Unauthorized, reusedTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Reset_com_token_adulterado_deve_retornar_nao_autorizado()
    {
        var user = await AuthTestHelper.CreateUserAsync(Factory);
        var resetToken = await AuthTestHelper.CreatePasswordResetTokenAsync(
            Factory,
            user.Id);

        var response = await Client.PostAsJsonAsync(
            "/auth/reset-password",
            new ResetPasswordDto
            {
                Token = AuthTestHelper.TamperToken(resetToken),
                NewPassword = "nova-senha-valida"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
