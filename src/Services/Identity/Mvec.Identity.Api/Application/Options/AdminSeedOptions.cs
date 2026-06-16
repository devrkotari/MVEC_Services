namespace Mvec.Identity.Api.Application.Options;

/// <summary>Seed data for the bootstrap admin account (SCR-A01 — admins are seeded, never self-registered).</summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = "admin@mvec.local";
    public string Password { get; set; } = "Admin#12345";
    public string FirstName { get; set; } = "Platform";
    public string LastName { get; set; } = "Admin";

    /// <summary>Optional fixed Base32 TOTP secret. If empty a new one is generated and logged once.</summary>
    public string? TwoFactorSecret { get; set; }

    public bool EnableTwoFactor { get; set; } = true;
}
