using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YourProject.Utils;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwt;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwt,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _jwt = jwt.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ResponseLoginDto> LoginAsync(LoginDto dto, HttpContext httpContext)
    {   
        _logger.LogInformation("Authentication started");

        var httpContextExist = _httpContextAccessor.HttpContext;

        if (httpContextExist != null)
        {
            var existingRefreshToken = httpContext.Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(existingRefreshToken))
            {
                try
                {
                    var principal = _tokenService.ValidateRefreshToken(existingRefreshToken);


                    var tokenIdClaim = principal.Claims
                        .FirstOrDefault(c => c.Type == "tokenId")?.Value;


                    if (Guid.TryParse(tokenIdClaim, out var tokenId))
                    {
                        var storedToken = await _context.RefreshTokens
                            .FirstOrDefaultAsync(x => x.Id == tokenId);

                        if (storedToken != null)
                        {
                            var incomingHash = Convert.ToHexString(
                                SHA256.HashData(Encoding.UTF8.GetBytes(existingRefreshToken))
                            );

                            var storedHash = storedToken.TokenHash;

                            var storedHashBytes = Convert.FromHexString(storedHash);
                            var incomingHashBytes = Convert.FromHexString(incomingHash);

                            var isValid = CryptographicOperations.FixedTimeEquals(
                                storedHashBytes,
                                incomingHashBytes
                            );

                            if (isValid)
                            {
                                storedToken.Status = TokenStatusEnum.revoked;
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Existing refresh token revoked");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid refresh token during login");
                }

                httpContext.Response.Cookies.Delete("refresh_token");
                httpContext.Response.Cookies.Delete("access_token");
            }
        }

        
        var userExist = await _context.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (userExist == null)
        {
            _logger.LogWarning("Authentication failed: User not found");
            throw new UnauthorizedException("Email ou senha invalida");
        }


        bool validPass = BCrypt.Net.BCrypt.Verify(dto.Password, userExist.Password);

        if (!validPass)
        {
            _logger.LogWarning("Authentication failed: Invalid password");
            throw new UnauthorizedException("Email ou senha invalida");
        }

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
            await _context.SaveChangesAsync();
            _logger.LogInformation("Oldest session revoked due to max session limit");
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
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
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

        _logger.LogInformation("Authentication succeeded");

        var userInfo = new UserResponseDto
        {
            Id = userExist.Id,
            Name = userExist.Name,
            Email = userExist.Email,
            CreatedAt = userExist.CreatedAt,
            Roles = userExist.UserRoles.Select(ur => ur.Role.Roles.ToString()).ToList()
        };

        return new ResponseLoginDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            UserInfo = userInfo
        };
    }

    public async Task<string> RefreshAsync(HttpContext httpContext){
        _logger.LogInformation("Token refresh started");

        var refreshToken = httpContext.Request.Cookies["refresh_token"];

        var handler = new JwtSecurityTokenHandler();

        var token = handler.ReadJwtToken(refreshToken);

        var userIdString = token.Claims
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
            ?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("Token refresh failed: Invalid token format");
            throw new UnauthorizedException("Refresh token inválido.");
        }

        var user = await _context.Users
        .Include(x => x.UserRoles)
        .ThenInclude(x => x.Role)
        .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            _logger.LogWarning("Token refresh failed: User not found");
            throw new UnauthorizedException("Usuário não encontrado.");
        }

        var newToken = _tokenService.CreateToken(user);
        _logger.LogInformation("Token refresh succeeded");
        return newToken;
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        _logger.LogInformation("Logout started");

        var existingRefreshToken = httpContext.Request.Cookies?["refresh_token"];

        if (!string.IsNullOrEmpty(existingRefreshToken))
        {
            try
            {
                var principal = _tokenService.ValidateRefreshToken(existingRefreshToken);


                var tokenIdClaim = principal.Claims
                    .FirstOrDefault(c => c.Type == "tokenId")?.Value;


                if (Guid.TryParse(tokenIdClaim, out var tokenId))
                {
                    var storedToken = await _context.RefreshTokens
                        .FirstOrDefaultAsync(x => x.Id == tokenId);

                    if (storedToken != null)
                    {
                        var incomingHash = Convert.ToHexString(
                            SHA256.HashData(Encoding.UTF8.GetBytes(existingRefreshToken))
                        );

                        var storedHash = storedToken.TokenHash;

                        var storedHashBytes = Convert.FromHexString(storedHash);
                        var incomingHashBytes = Convert.FromHexString(incomingHash);

                        var isValid = CryptographicOperations.FixedTimeEquals(
                            storedHashBytes,
                            incomingHashBytes
                        );

                        if (isValid)
                        {
                            storedToken.Status = TokenStatusEnum.revoked;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Refresh token revoked during logout");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token during logout");
            }
        }

        httpContext.Response.Cookies.Delete("refresh_token");
        httpContext.Response.Cookies.Delete("access_token");

        _logger.LogInformation("Logout succeeded");
    }
}