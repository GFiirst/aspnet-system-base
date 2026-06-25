namespace YourProject.Utils;

public static class DeviceHelper
{
    public static string ExtractDevice(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "unknown";

        if (userAgent.Contains("Mobile"))
            return "Mobile";

        if (userAgent.Contains("Windows"))
            return "Windows";

        if (userAgent.Contains("Linux"))
            return "Linux";

        if (userAgent.Contains("Mac"))
            return "Mac";

        return "Unknown";
    }
}