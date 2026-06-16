using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Mvec.Vendor.Api.Application.Abstractions;

namespace Mvec.Vendor.Api.Infrastructure.Security;

/// <summary>Reads the authenticated principal from the current HTTP request (JWT issued by Identity).</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public long? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public string? Role =>
        Principal?.FindFirstValue(ClaimTypes.Role)
        ?? Principal?.FindFirstValue("role");
}
