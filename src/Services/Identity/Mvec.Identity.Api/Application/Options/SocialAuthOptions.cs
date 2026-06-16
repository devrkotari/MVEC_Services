namespace Mvec.Identity.Api.Application.Options;

/// <summary>Expected client IDs (audiences) for validating social provider id-tokens.</summary>
public sealed class SocialAuthOptions
{
    public const string SectionName = "SocialAuth";

    public string? GoogleClientId { get; set; }
    public string? FacebookAppId { get; set; }
}
