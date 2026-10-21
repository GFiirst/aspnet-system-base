using System.Net;
using System.Net.Http.Json;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class ForgotPasswordTests : IntegrationTestBase
{
    public ForgotPasswordTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Forgot_password_nao_deve_revelar_que_email_nao_existe()
    {
        const string unknownEmail = "inexistente@system-base.test";

        var response = await Client.PostAsJsonAsync(
            "/auth/forgot-password",
            new ForgotPasswordDto { Email = unknownEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "Se existir uma conta vinculada",
            await response.Content.ReadAsStringAsync());

        var logs = string.Join(Environment.NewLine, Factory.AuthLogger.Messages);
        Assert.Contains("Forgot password request started", logs);
        Assert.DoesNotContain(
            unknownEmail,
            logs,
            StringComparison.OrdinalIgnoreCase);
    }
}
