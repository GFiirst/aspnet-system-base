public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool>CreateUserAsync(CreateUserDto dto)
    {
        return true;
    }
}