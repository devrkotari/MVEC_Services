namespace Mvec.Identity.Api.Application.Options;

/// <summary>Strongly-typed binding of the "Jwt" configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "mvec";
    public string Audience { get; set; } = "mvec";

    /// <summary>Symmetric signing key. In production this is sourced from Key Vault.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes (default 15, per Guide §4).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh-token lifetime in days (default 7, per Guide §4).</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
