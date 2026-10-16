using System.Net;
using System.Net.Http.Json;
using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration.Auth;

public sealed class AuthRateLimitTests : IntegrationTestBase
{
    public AuthRateLimitTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_deve_bloquear_a_decima_primeira_tentativa_na_janela()
    {
        var payload = new LoginDto
        {
            Email = "inexistente@system-base.test",
            Password = "senha-invalida"
        };

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await Client.PostAsJsonAsync("/auth/login", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var blockedResponse = await Client.PostAsJsonAsync("/auth/login", payload);

        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
    }
}
