using Microsoft.AspNetCore.Mvc;

namespace Mvec.Product.Api.Api;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    // TODO: implement endpoints per Guide. Placeholder health probe below.
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { service = "Product", status = "ok" });
}
