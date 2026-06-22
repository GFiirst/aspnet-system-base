public class JwtSettings
{
    public string AccessKey { get; set; } = "";
    public string RefreshKey { get; set; } = "";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    public int AccessExpirationMinutes { get; set; }
    public int RefreshExpirationDays { get; set; }
}