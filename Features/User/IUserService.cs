public interface IUserService
{
    Task<UserResponseDto> CreateUserAsync(CreateUserDto dto);
}