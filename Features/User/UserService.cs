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
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12)
        };

        _context.Users.Add(user);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Roles == RolesEnum.user);

        if(role == null)
        {
            throw new InvalidOperationException("Role não encontrada.");
        }

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}