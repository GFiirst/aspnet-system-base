using Microsoft.EntityFrameworkCore;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    private readonly ILogger<UserService> _logger;

    private readonly IEncryptionService _encryptionService;

    public UserService(AppDbContext context, ILogger<UserService> logger, IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
    {
        _logger.LogInformation("User creation started");

        var normalizedEmail = dto.Email.ToLowerInvariant();

        var emailHash = _encryptionService.ComputeHash(normalizedEmail);

        var userExist = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash);

        if(userExist != null)
        {   
            _logger.LogWarning("User creation failed: Email already exists");
            throw new ConflictException("Email já existe.");
        }

        var encryptedEmail = _encryptionService.Encrypt(normalizedEmail);

        var user = new User
        {   
            Id = Guid.NewGuid(),
            Name = dto.Name,
            EmailEncrypted = encryptedEmail,
            EmailHash = emailHash,
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

        var decryptedEmail = _encryptionService.Decrypt(user.EmailEncrypted);

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = decryptedEmail,
            CreatedAt = user.CreatedAt,
            Roles = [role.Roles.ToString()]
        };
    }
}