using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateUserAsync(CreateUserDto dto)
    {
        var userExist = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if(userExist != null)
        {
            throw new ConflictException("Email já existe.");
        }

        var user = new User
        {   
            Name = dto.Name,
            Email = dto.Email,
            Password = PasswordHasher.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}