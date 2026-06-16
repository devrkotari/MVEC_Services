using Microsoft.AspNetCore.Mvc;

namespace Mvec.Analytics.Api.Api;

[ApiController]
[Route("api/analytics")]
public class SalesFactController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Analytics", status = "ok" });
}
