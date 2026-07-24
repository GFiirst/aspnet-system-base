public class User : BaseEntity
{
    public string Name {get; set;} = "";

    public string EmailEncrypted {get; set;} = "";
    public string EmailHash {get; set;} = "";

    public string Password {get; set;} = "";

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}