using Microsoft.AspNetCore.Mvc;

namespace Mvec.Notification.Api.Api;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Notification", status = "ok" });
}
