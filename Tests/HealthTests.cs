using AspnetSystemBase.Tests.Infrastructure;

namespace AspnetSystemBase.Tests.Integration;

public sealed class HealthTests : IntegrationTestBase
{
    public HealthTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Health_deve_retornar_sucesso()
    {
        var response = await Client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
