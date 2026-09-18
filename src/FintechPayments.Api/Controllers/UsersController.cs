using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FintechPayments.Api.Authentication;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        User.TryGetUserId(out Guid userId);
        string? role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return Ok(new { userId, role });
    }
}
