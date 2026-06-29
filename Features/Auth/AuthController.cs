using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public AuthController
    (
        IUserService userService,
        IAuthService authService
    )
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {   
        await _userService.CreateUserAsync(dto);
        return Created();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto, HttpContext);

        Response.Cookies.Append(
            "access_token",
            result.AccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

        Response.Cookies.Append(
            "refresh_token",
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        return NoContent();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [RefreshTokenAuthorize]
    public async Task<IActionResult> RefreshToken()
    {
        var accessToken = await _authService.RefreshAsync(HttpContext);

        Response.Cookies.Append(
            "access_token",
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

        return NoContent();
    }

    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {   
        await _authService.LogoutAsync(HttpContext);
        return NoContent();
    }

    [HttpGet("teste")]
    [Authorize(Policy = Policies.UserCreate)]
    public async Task<IActionResult> testeToken()
    {
       return Ok("passou");
    }

}
