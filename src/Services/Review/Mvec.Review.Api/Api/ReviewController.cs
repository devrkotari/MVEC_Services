using Microsoft.AspNetCore.Mvc;

namespace Mvec.Review.Api.Api;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Review", status = "ok" });
}
