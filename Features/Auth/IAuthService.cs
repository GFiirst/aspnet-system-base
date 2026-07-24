public interface IAuthService
{
    Task<ResponseLoginDto> LoginAsync(LoginDto dto,  HttpContext httpContext);

    Task<string> RefreshAsync(HttpContext httpContext);

    Task LogoutAsync(HttpContext httpContext);

    Task ForgotPasswordAsync(ForgotPasswordDto dto);

    Task ResetPasswordAsync(ResetPasswordDto dto);
}