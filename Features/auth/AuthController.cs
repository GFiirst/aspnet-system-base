using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController
    (
        IUserService userService
    )
    {
        _userService = userService;
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {   
        await _userService.CreateUserAsync(dto);
        return Created();
    }
}