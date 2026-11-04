using AspnetSystemBase.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AspnetSystemBase.Tests.Shared;

public sealed class AccentInsensitiveSearchTests : IntegrationTestBase
{
    public AccentInsensitiveSearchTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Pesquisa_deve_ignorar_acentuacao()
    {
        var user = await AuthTestHelper.CreateUserAsync(Factory);
        var pattern = QueryFilterHelper.CreateContainsPattern("usuario");

        var foundId = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.Users
                .Where(item => QueryFilterHelper.UnaccentedILike(
                    item.Name,
                    pattern!,
                    QueryFilterHelper.LikeEscapeCharacter))
                .Select(item => item.Id)
                .SingleAsync());

        Assert.Equal(user.Id, foundId);
    }
}
