using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Contracts;

namespace Mvec.Identity.Api.Api;

[ApiController]
[Route("api/identity")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookie = "mvec_refresh";

    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    /// <summary>Buyer / vendor self-registration. Publishes <c>UserRegistered</c>.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        return result.ToCreated("/api/identity/me");
    }

    /// <summary>Password login. Returns tokens, or a 2FA challenge for admin accounts.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        if (result.IsSuccess && result.Value!.Tokens is { } tokens)
            SetRefreshCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        return result.ToOk();
    }

    /// <summary>Completes an admin login by verifying the TOTP code.</summary>
    [HttpPost("login/2fa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginTwoFactor(TwoFactorLoginRequest request, CancellationToken ct)
    {
        var result = await _auth.CompleteTwoFactorAsync(request, ct);
        if (result.IsSuccess && result.Value!.Tokens is { } tokens)
            SetRefreshCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
        return result.ToOk();
    }

    /// <summary>Rotates tokens. Reads the refresh token from cookie or body.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        var raw = request?.RefreshToken ?? Request.Cookies[RefreshCookie];
        var result = await _auth.RefreshAsync(raw, ct);
        if (result.IsSuccess)
            SetRefreshCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return result.ToOk();
    }

    /// <summary>Revokes the current refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        var raw = request?.RefreshToken ?? Request.Cookies[RefreshCookie];
        var result = await _auth.LogoutAsync(_currentUser.UserId!.Value, raw, ct);
        ClearRefreshCookie();
        return result.ToOk();
    }

    [HttpPost("social/google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    public Task<IActionResult> Google(SocialLoginRequest request, CancellationToken ct) =>
        SocialAsync("google", request, ct);

    [HttpPost("social/facebook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    public Task<IActionResult> Facebook(SocialLoginRequest request, CancellationToken ct) =>
        SocialAsync("facebook", request, ct);

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        var result = await _auth.VerifyEmailAsync(request, ct);
        return result.ToOk();
    }

    private async Task<IActionResult> SocialAsync(string provider, SocialLoginRequest request, CancellationToken ct)
    {
        var result = await _auth.SocialLoginAsync(provider, request, ct);
        if (result.IsSuccess)
            SetRefreshCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return result.ToOk();
    }

    private void SetRefreshCookie(string token, DateTime expiresAt) =>
        Response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/api/identity"
        });

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/identity" });
}
