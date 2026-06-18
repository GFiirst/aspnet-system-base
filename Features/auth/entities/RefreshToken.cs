public class RefreshToken : BaseEntity
{
    public string Token {get; set;} = "";

    public DateTime ExpiredAt{get; set;}

    public TokenStatusEnum Status {get; set;}

    public string UserAgent {get; set;} = "";

    public string Device {get; set;} = "";

    public string Ip {get; set;} = "";

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}