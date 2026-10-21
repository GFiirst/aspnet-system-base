using System.Text.Json.Nodes;
using AspnetSystemBase.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AspnetSystemBase.Tests.Integration.Audit;

public sealed class AuditInterceptorTests : IntegrationTestBase
{
    public AuditInterceptorTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Criacao_de_usuario_deve_persistir_log_sem_dados_sensiveis()
    {
        await AuthTestHelper.CreateUserAsync(Factory);

        var newValues = await AuthTestHelper.ExecuteDbAsync(
            Factory,
            context => context.AuditLogs
                .Where(log => log.EntityName == nameof(User))
                .Select(log => log.NewValues)
                .SingleAsync());
        var node = JsonNode.Parse(Assert.IsType<string>(newValues))!;

        Assert.Equal("Usuário de Teste", node["Name"]!.GetValue<string>());
        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            node["Password"]!.GetValue<string>());
        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            node["EmailEncrypted"]!.GetValue<string>());
        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            node["EmailHash"]!.GetValue<string>());
        Assert.DoesNotContain(AuthTestHelper.DefaultEmail, newValues);
        Assert.DoesNotContain(AuthTestHelper.DefaultPassword, newValues);
    }
}
