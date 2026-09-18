using FintechPayments.Api.Contracts.Authentication;
using FintechPayments.Application.Authentication;
using FintechPayments.Application.Auditing;
using FintechPayments.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
public sealed class AuthController(AuthenticationService authentication, AuditService audit) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthenticationResult result = await authentication.RegisterAsync(
                request.Email, request.Password, cancellationToken);

            if (!result.Succeeded)
            {
                await audit.RecordAsync(null, "user.register", "User", null,
                    "rejected", HttpContext.TraceIdentifier, result.ErrorCode, CancellationToken.None);
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Email is already registered.",
                    extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
            }

            await audit.RecordAsync(result.UserId, "user.register", "User", result.UserId,
                "succeeded", HttpContext.TraceIdentifier, null, CancellationToken.None);
            return StatusCode(StatusCodes.Status201Created, ToResponse(result));
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Registration is invalid.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        AuthenticationResult result = await authentication.LoginAsync(
            request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
        {
            await audit.RecordAsync(null, "user.login", "User", null,
                "rejected", HttpContext.TraceIdentifier, result.ErrorCode, CancellationToken.None);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed.",
                extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
        }

        await audit.RecordAsync(result.UserId, "user.login", "User", result.UserId,
            "succeeded", HttpContext.TraceIdentifier, null, CancellationToken.None);
        return Ok(ToResponse(result));
    }

    private static AuthenticationResponse ToResponse(AuthenticationResult result) => new(
        result.UserId!.Value,
        result.Email!,
        result.Role!,
        result.Token!.Value,
        result.Token.ExpiresAt);
}
