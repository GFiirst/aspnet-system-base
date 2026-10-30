using System.Net;
using YourProject.Utils;

namespace AspnetSystemBase.Tests.Unit.Shared;

public sealed class ClientIpAddressHelperTests
{
    [Theory]
    [InlineData("::ffff:172.18.0.1", "172.18.0.1")]
    [InlineData("177.65.144.63", "177.65.144.63")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    public void Deve_normalizar_apenas_ipv4_mapeado_em_ipv6(
        string input,
        string expected)
    {
        var result = ClientIpAddressHelper.ToNormalizedString(
            IPAddress.Parse(input));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("::ffff:172.23.0.1", "172.23.0.1")]
    [InlineData("unknown", "unknown")]
    public void Deve_normalizar_texto_legado_sem_alterar_valor_invalido(
        string input,
        string expected)
    {
        Assert.Equal(expected, ClientIpAddressHelper.NormalizeString(input));
    }

    [Fact]
    public void Deve_preservar_ip_ausente()
    {
        Assert.Null(ClientIpAddressHelper.ToNormalizedString(null));
    }
}
