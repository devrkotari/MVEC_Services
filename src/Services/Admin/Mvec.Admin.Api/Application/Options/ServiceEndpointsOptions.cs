namespace Mvec.Admin.Api.Application.Options;

/// <summary>
/// Base URLs of the downstream services the Admin BFF calls directly (service-to-service,
/// not through the gateway). Bound from the "Services" config section.
/// </summary>
public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "Services";

    public string Identity { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
}
