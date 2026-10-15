using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YourProject.Utils;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MimeKit;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwt;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly IEncryptionService _encryptionService;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwt,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger,
        IOptions<EmailSettings> emailSettings,
        IEncryptionService encryptionService)
    {
        _context = context;
        _tokenService = tokenService;
        _jwt = jwt.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _encryptionService = encryptionService;
    }

    public async Task<LoginResult> LoginAsync(LoginDto dto, HttpContext httpContext)
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

        
        var normalizedEmail = dto.Email.ToLowerInvariant();
        var emailHash = _encryptionService.ComputeHash(normalizedEmail);

        var userExist = await _context.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash);

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
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessExpirationMinutes);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshExpirationDays);

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userExist.Id,
            UserAgent = userAgent,
            IpEncrypted = _encryptionService.Encrypt(ipAddress),
            IpHash = _encryptionService.ComputeHash(ipAddress),
            Device = DeviceHelper.ExtractDevice(userAgent),
            ExpiredAt = refreshExpiresAt,
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
                expires: refreshExpiresAt,
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

        var decryptedEmail = _encryptionService.Decrypt(userExist.EmailEncrypted);

        var userInfo = new UserResponseDto
        {
            Id = userExist.Id,
            Name = userExist.Name,
            Email = decryptedEmail,
            CreatedAt = userExist.CreatedAt,
            Roles = userExist.UserRoles.Select(ur => ur.Role.Roles.ToString()).ToList()
        };

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessExpiresAt,
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

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        _logger.LogInformation("Forgot password request started for email: {Email}", dto.Email);

        var normalizedEmail = dto.Email.ToLowerInvariant();
        var emailHash = _encryptionService.ComputeHash(normalizedEmail);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash);

        if (user == null)
        {
            _logger.LogInformation("User not found for email: {Email}", dto.Email);
            return;
        }

        var token = GeneratePasswordResetToken(user);

        var resetLink = $"{_emailSettings.FrontendUrl}/reset-password?token={token}";

        var decryptedEmail = _encryptionService.Decrypt(user.EmailEncrypted);
        await SendPasswordResetEmailAsync(decryptedEmail, resetLink);

        _logger.LogInformation("Password reset email sent for email: {Email}", dto.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        _logger.LogInformation("Password reset request started");

        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal;

        try
        {
            principal = handler.ValidateToken(
                dto.Token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = _jwt.Issuer,
                    ValidAudience = _jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_jwt.ResetPasswordKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero
                },
                out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _logger.LogWarning(ex, "Invalid or expired token during password reset");
            throw new UnauthorizedException("Token inválido ou expirado.");
        }

        var purposeClaim = principal.FindFirst("purpose")?.Value;
        if (purposeClaim != "password-reset")
        {
            _logger.LogWarning("Invalid token purpose during password reset");
            throw new UnauthorizedException("Token inválido.");
        }

        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("Invalid user ID in token during password reset");
            throw new UnauthorizedException("Token inválido.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            _logger.LogWarning("User not found during password reset");
            throw new UnauthorizedException("Usuário não encontrado.");
        }

        var passwordVersion = principal.FindFirst("password-version")?.Value;
        var currentPasswordVersion = _encryptionService.ComputeHash(user.Password);

        if (!FixedTimeHashEquals(passwordVersion, currentPasswordVersion))
        {
            _logger.LogWarning(
                "Password reset token was already used or invalidated for user: {UserId}",
                userId);
            throw new UnauthorizedException("Token inválido ou já utilizado.");
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.Password = hashedPassword;

        var activeRefreshTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.Status == TokenStatusEnum.active)
            .ToListAsync();

        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Status = TokenStatusEnum.revoked;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrent reuse of password reset token detected for user: {UserId}",
                userId);
            throw new UnauthorizedException("Token inválido ou já utilizado.");
        }

        _logger.LogInformation("Password reset completed for user: {UserId}", userId);
    }

    public async Task<ValidateResponseDto> ValidateAsync(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Token inválido.");
        }

        var user = await _context.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            throw new UnauthorizedException("Usuário não encontrado.");
        }

        return new ValidateResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = _encryptionService.Decrypt(user.EmailEncrypted),
            Roles = user.UserRoles.Select(x => x.Role.Roles.ToString()).ToList()
        };
    }

    private string GeneratePasswordResetToken(User user)
    {
        var jti = Guid.NewGuid().ToString();
        var passwordVersion = _encryptionService.ComputeHash(user.Password);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("purpose", "password-reset"),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim("password-version", passwordVersion)
            },
            expires: DateTime.UtcNow.AddMinutes(_jwt.ResetPasswordExpirationMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.ResetPasswordKey)),
                SecurityAlgorithms.HmacSha256
            )
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool FixedTimeHashEquals(string? first, string second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(first),
                Convert.FromHexString(second));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PAS System", _emailSettings.MailUser));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Recuperação de Senha";

            var body = PasswordResetEmailTemplate.Generate(resetLink);

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.MailUser, _emailSettings.MailPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to: {Email}", email);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing email for: {Email}", email);
            throw;
        }
    }
}
