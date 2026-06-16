using System.Text.Json;
using Mvec.Identity.Api.Application.Abstractions;

namespace Mvec.Identity.Api.Infrastructure.Social;

/// <summary>
/// Validates a Facebook user access-token by calling the Graph API /me endpoint.
/// A successful response with a stable id proves the token is valid and not expired.
/// </summary>
public sealed class FacebookAuthValidator : IExternalAuthValidator
{
    public string Provider => "facebook";

    private readonly HttpClient _http;

    public FacebookAuthValidator(HttpClient http) => _http = http;

    public async Task<ExternalUserInfo?> ValidateAsync(string token, CancellationToken ct = default)
    {
        var url = $"https://graph.facebook.com/me?fields=id,name,email&access_token={Uri.EscapeDataString(token)}";
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var id = root.TryGetProperty("id", out var i) ? i.GetString() : null;
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email)) return null;

        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        return new ExternalUserInfo(Provider, id, email, name ?? email);
    }
}
