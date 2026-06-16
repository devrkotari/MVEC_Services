using Microsoft.AspNetCore.Mvc;

namespace Mvec.Order.Api.Api;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Order", status = "ok" });
}
