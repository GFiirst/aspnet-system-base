using System.Net;

namespace YourProject.Utils;

public static class ClientIpAddressHelper
{
    public static IPAddress? Normalize(IPAddress? ipAddress) =>
        ipAddress?.IsIPv4MappedToIPv6 == true
            ? ipAddress.MapToIPv4()
            : ipAddress;

    public static string? ToNormalizedString(IPAddress? ipAddress) =>
        Normalize(ipAddress)?.ToString();

    public static string? NormalizeString(string? ipAddress) =>
        IPAddress.TryParse(ipAddress, out var parsedAddress)
            ? ToNormalizedString(parsedAddress)
            : ipAddress;
}
