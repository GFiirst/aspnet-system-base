public interface IAuthService
{
    Task<ResponseLoginDto> LoginAsync(LoginDto dto,  HttpContext httpContext);

    Task<string> RefreshAsync(HttpContext httpContext);
}