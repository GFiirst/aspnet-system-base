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
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {   
        await _userService.CreateUserAsync(dto);
        return Created();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {   
        await _authService.LoginAsync(dto);

        return Created();
    }
}