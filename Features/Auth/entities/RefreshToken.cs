public class RefreshToken : BaseEntity
{
    public string TokenHash {get; set;} = "";

    public DateTime ExpiredAt{get; set;}

    public TokenStatusEnum Status {get; set;}

    public string? UserAgent {get; set;}

    public string? Device {get; set;}

    public string? IpEncrypted {get; set;}

    public string? IpHash {get; set;}

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
