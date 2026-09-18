using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FintechPayments.Application.Abstractions.Authentication;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Users;
using FintechPayments.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FintechPayments.Infrastructure.Authentication;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    public AuthenticationToken Create(User user)
    {
        JwtOptions settings = options.Value;
        DateTimeOffset expiresAt = clock.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(settings.SigningKey));

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Role, user.Role.ToString()),
        ];

        JwtSecurityToken token = new(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthenticationToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
