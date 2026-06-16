namespace Mvec.Identity.Api.Domain;

/// <summary>Links a user to a third-party identity provider account (idn.ExternalLogins).</summary>
public class ExternalLogin
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    private ExternalLogin() { }

    public static ExternalLogin Create(long userId, string provider, string providerKey) => new()
    {
        UserId = userId,
        Provider = provider,
        ProviderKey = providerKey
    };
}
