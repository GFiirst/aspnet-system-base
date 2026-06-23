using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

public class RefreshTokenAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        var dbContext = httpContext.RequestServices
            .GetRequiredService<AppDbContext>();

        var jwtOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<JwtSettings>>();

        var jwt = jwtOptions.Value;

        var refreshToken = httpContext.Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new UnauthorizedException("Refresh token invalido");
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.RefreshKey))
            };

            var principal = tokenHandler.ValidateToken(
                refreshToken,
                validationParameters,
                out _);

            var tokenIdClaim = principal.FindFirst("tokenId")?.Value;

            if (!Guid.TryParse(tokenIdClaim, out var tokenId))
            {
                throw new UnauthorizedException("Refresh token invalido");
            }

            var storedToken = await dbContext.RefreshTokens.FindAsync(tokenId);

            if (storedToken is null || storedToken.Status != TokenStatusEnum.active)
            {
                throw new UnauthorizedException("Refresh token invalido");
            }

            var incomingHash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            var storedHash = Convert.FromHexString(storedToken.TokenHash);

            if (!CryptographicOperations.FixedTimeEquals(storedHash, incomingHash))
            {
                throw new UnauthorizedException("Refresh token invalido");
            }

            if (storedToken.ExpiredAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token invalido");
            }

            httpContext.Items["RefreshToken"] = storedToken;

            await next();
        }
        catch
        {
            throw new UnauthorizedException("Refresh token invalido");
        }
    }
}