public class User : BaseEntity
{
    public string Name {get; set;} = "";

    public string Email {get; set;} = "";

    public string Password {get; set;} = "";

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}