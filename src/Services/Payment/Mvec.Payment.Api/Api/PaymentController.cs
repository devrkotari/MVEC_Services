using Microsoft.AspNetCore.Mvc;

namespace Mvec.Payment.Api.Api;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Payment", status = "ok" });
}
