using System.Text.Json.Nodes;

namespace AspnetSystemBase.Tests.Unit.Audit;

public sealed class AuditValueSanitizerTests
{
    [Theory]
    [InlineData("Password")]
    [InlineData("PasswordHash")]
    [InlineData("RefreshToken")]
    [InlineData("reset_token_hash")]
    [InlineData("clientSecret")]
    [InlineData("EmailEncrypted")]
    [InlineData("Auth")]
    [InlineData("UserAgent")]
    public void Deve_proteger_campos_sensiveis_independentemente_de_maiusculas(
        string propertyName)
    {
        var result = AuditValueSanitizer.SanitizeValue(propertyName, "segredo");

        Assert.Equal(AuditValueSanitizer.ProtectedValue, result);
    }

    [Fact]
    public void Deve_preservar_campos_nao_sensiveis()
    {
        var result = AuditValueSanitizer.SanitizeValue("Name", "Maria");

        Assert.Equal("Maria", result);
    }

    [Fact]
    public void Deve_sanitizar_campos_sensiveis_em_json_aninhado()
    {
        const string json =
            """{"Name":"Maria","Password":"senha","nested":{"accessToken":"jwt","Value":"visivel"}}""";

        var sanitized = AuditValueSanitizer.SanitizeJson(json);
        var node = JsonNode.Parse(Assert.IsType<string>(sanitized))!;

        Assert.Equal("Maria", node["Name"]!.GetValue<string>());
        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            node["Password"]!.GetValue<string>());
        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            node["nested"]!["accessToken"]!.GetValue<string>());
        Assert.Equal("visivel", node["nested"]!["Value"]!.GetValue<string>());
        Assert.DoesNotContain("senha", sanitized);
        Assert.DoesNotContain("jwt", sanitized);
    }

    [Fact]
    public void Deve_sanitizar_campos_sensiveis_dentro_de_arrays()
    {
        const string json =
            """{"items":[{"clientSecret":"segredo"},{"Name":"visivel"}]}""";

        var sanitized = AuditValueSanitizer.SanitizeJson(json);
        var node = JsonNode.Parse(Assert.IsType<string>(sanitized))!;
        var items = node["items"]!.AsArray();

        Assert.Equal(
            AuditValueSanitizer.ProtectedValue,
            items[0]!["clientSecret"]!.GetValue<string>());
        Assert.Equal("visivel", items[1]!["Name"]!.GetValue<string>());
        Assert.DoesNotContain("segredo", sanitized);
    }

    [Fact]
    public void Json_invalido_nao_deve_ser_exposto_pela_auditoria()
    {
        Assert.Null(AuditValueSanitizer.SanitizeJson("{invalido"));
    }
}
