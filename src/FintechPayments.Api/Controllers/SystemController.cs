using Microsoft.AspNetCore.Mvc;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo() =>
        Ok(new SystemInfoResponse("Fintech Transaction API", "v1"));

    [HttpGet("failure-probe")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FailureProbe()
    {
        if (!environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        throw new InvalidOperationException("Integration-test exception probe.");
    }
}

public sealed record SystemInfoResponse(string Name, string ApiVersion);
