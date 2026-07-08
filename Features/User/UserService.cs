using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
    {
        _logger.LogInformation("User creation started");

        var userExist = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if(userExist != null)
        {   
            _logger.LogWarning("User creation failed: Email already exists");
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
            _logger.LogError("User creation failed: Role not found");
            throw new InvalidOperationException("Role não encontrada.");
        }

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("User creation succeeded");
        await _context.SaveChangesAsync();

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Roles = [role.Roles.ToString()]
        };
    }
}