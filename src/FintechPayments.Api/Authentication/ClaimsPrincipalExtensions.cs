using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FintechPayments.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out userId);
    }
}
