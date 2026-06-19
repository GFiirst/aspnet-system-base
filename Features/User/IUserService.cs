public interface IUserService
{
    Task<bool> CreateUserAsync(CreateUserDto dto);
}