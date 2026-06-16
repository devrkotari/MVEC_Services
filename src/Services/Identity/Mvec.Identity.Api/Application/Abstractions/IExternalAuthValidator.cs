namespace Mvec.Identity.Api.Application.Abstractions;

/// <summary>Verified identity returned by a social provider after validating its id-token.</summary>
public sealed record ExternalUserInfo(string Provider, string ProviderKey, string Email, string DisplayName);

/// <summary>Validates a provider-issued id-token / access-token and returns the verified identity.</summary>
public interface IExternalAuthValidator
{
    /// <summary>Provider key this validator handles, e.g. "google" or "facebook".</summary>
    string Provider { get; }

    Task<ExternalUserInfo?> ValidateAsync(string token, CancellationToken ct = default);
}
