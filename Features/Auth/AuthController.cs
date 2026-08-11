using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    public AuthController
    (
        IUserService userService,
        IAuthService authService,
        IConfiguration configuration
    )
    {
        _userService = userService;
        _authService = authService;
        _configuration = configuration;
    }

    private bool GetSecureCookieSetting()
    {
        return _configuration.GetValue<bool>("CookieSettings:Secure", true);
    }

    [HttpPost("sign-up")]
    [AllowAnonymous]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {   
        return Ok(await _userService.CreateUserAsync(dto));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto, HttpContext);

        Response.Cookies.Append(
            "access_token",
            result.AccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = GetSecureCookieSetting(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

        Response.Cookies.Append(
            "refresh_token",
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = GetSecureCookieSetting(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [RefreshTokenAuthorize]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> RefreshToken()
    {
        var accessToken = await _authService.RefreshAsync(HttpContext);

        Response.Cookies.Append(
            "access_token",
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = GetSecureCookieSetting(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

        var result = new RefreshResponseDto
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> Logout()
    {   
        await _authService.LogoutAsync(HttpContext);
        return Ok();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "Se existir uma conta vinculada a este e-mail, uma mensagem de recuperação será enviada." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("Default")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(new { message = "Senha redefinida com sucesso." });
    }
}
