using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YourProject.Utils;
using System.Security.Claims;
using Microsoft.Extensions.Options;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwt;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwt)
    {
        _context = context;
        _tokenService = tokenService;
        _jwt = jwt.Value;
    }

    public async Task<ResponseLoginDto> LoginAsync(LoginDto dto, HttpContext httpContext)
    {
        var userExist = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (userExist == null)
            throw new UnauthorizedException("Email ou senha invalida");


        bool validPass = BCrypt.Net.BCrypt.Verify(dto.Password, userExist.Password);

        if (!validPass)
            throw new UnauthorizedException("Email ou senha invalida");

        const int maxSession = 5;

        var sessionCount = await _context.RefreshTokens
            .CountAsync(t => t.UserId == userExist.Id &&
                            t.Status == TokenStatusEnum.active);

        if (sessionCount >= maxSession)
        {
            var oldestSession = await _context.RefreshTokens
                .Where(t => t.UserId == userExist.Id &&
                            t.Status == TokenStatusEnum.active)
                .OrderBy(t => t.CreatedAt)
                .FirstAsync();

            oldestSession.Status = TokenStatusEnum.revoked;
        }

        var accessToken = _tokenService.CreateToken(userExist);

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userExist.Id,
            UserAgent = userAgent,
            Ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Device = DeviceHelper.ExtractDevice(userAgent),
            ExpiredAt = DateTime.UtcNow.AddDays(30),
        };

        var refreshToken = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(
                claims:
                [
                    new Claim(JwtRegisteredClaimNames.Sub, userExist.Id.ToString()),
                    new Claim("tokenId", refreshEntity.Id.ToString())
                ],
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_jwt.RefreshKey)
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            )
        );

        refreshEntity.TokenHash =
            Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes(refreshToken)
                )
            );

        _context.RefreshTokens.Add(refreshEntity);
        await _context.SaveChangesAsync();

        return new ResponseLoginDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
        };
    }
}