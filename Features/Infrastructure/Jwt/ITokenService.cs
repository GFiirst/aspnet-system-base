using System.Security.Claims;

public interface ITokenService
{
    string CreateToken(User user);

    ClaimsPrincipal ValidateRefreshToken(string token);
}