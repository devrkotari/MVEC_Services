using System.Text.Json;
using Microsoft.Extensions.Options;
using Mvec.Identity.Api.Application.Abstractions;
using Mvec.Identity.Api.Application.Options;

namespace Mvec.Identity.Api.Infrastructure.Social;

/// <summary>
/// Validates a Google id-token via Google's tokeninfo endpoint and checks the audience
/// matches our configured client id. (For high throughput, swap for local JWKS validation.)
/// </summary>
public sealed class GoogleAuthValidator : IExternalAuthValidator
{
    public string Provider => "google";

    private readonly HttpClient _http;
    private readonly SocialAuthOptions _options;

    public GoogleAuthValidator(HttpClient http, IOptions<SocialAuthOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<ExternalUserInfo?> ValidateAsync(string token, CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(token)}", ct);
        if (!resp.IsSuccessStatusCode) return null;

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var aud = root.TryGetProperty("aud", out var a) ? a.GetString() : null;
        if (!string.IsNullOrEmpty(_options.GoogleClientId) && aud != _options.GoogleClientId)
            return null;

        var emailVerified = root.TryGetProperty("email_verified", out var ev) &&
                            (ev.ValueKind == JsonValueKind.True ||
                             (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"));
        if (!emailVerified) return null;

        var sub = root.TryGetProperty("sub", out var s) ? s.GetString() : null;
        var email = root.TryGetProperty("email", out var e) ? e.GetString() : null;
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email)) return null;

        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        return new ExternalUserInfo(Provider, sub, email, name ?? email);
    }
}
