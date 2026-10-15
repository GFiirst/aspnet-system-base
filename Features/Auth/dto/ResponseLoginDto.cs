public class ResponseLoginDto
{
    public DateTime ExpiresAt { get; set; }
    public UserResponseDto UserInfo { get; set; } = null!;
}
