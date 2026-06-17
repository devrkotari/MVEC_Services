namespace Mvec.Admin.Api.Application.Abstractions;

/// <summary>A relayed downstream response: the HTTP status and raw JSON body, passed back verbatim.</summary>
public readonly record struct DownstreamResponse(int StatusCode, string? Content);

/// <summary>Calls the Identity service's admin endpoints (forwarding the caller's JWT).</summary>
public interface IIdentityAdminClient
{
    /// <summary>Total user count (for the dashboard) — reads PagedResult.Total from the users list.</summary>
    Task<int> GetUserCountAsync(CancellationToken ct = default);

    Task<DownstreamResponse> ListUsersAsync(string? queryString, CancellationToken ct = default);
    Task<DownstreamResponse> ChangeUserStatusAsync(long userId, string jsonBody, CancellationToken ct = default);
}

/// <summary>Calls the Vendor service's admin endpoints (forwarding the caller's JWT).</summary>
public interface IVendorAdminClient
{
    /// <summary>Count of vendors awaiting KYC review (for the dashboard) — reads PagedResult.Total.</summary>
    Task<int> GetPendingApprovalCountAsync(CancellationToken ct = default);

    Task<DownstreamResponse> ListPendingAsync(string? queryString, CancellationToken ct = default);
    Task<DownstreamResponse> GetDetailAsync(long vendorId, CancellationToken ct = default);
    Task<DownstreamResponse> ApproveAsync(long vendorId, CancellationToken ct = default);
    Task<DownstreamResponse> RejectAsync(long vendorId, string jsonBody, CancellationToken ct = default);
    Task<DownstreamResponse> SuspendAsync(long vendorId, CancellationToken ct = default);
    Task<DownstreamResponse> ReinstateAsync(long vendorId, CancellationToken ct = default);
}
