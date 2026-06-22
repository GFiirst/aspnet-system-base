public interface IAuthService
{
    Task LoginAsync(LoginDto dto);
}