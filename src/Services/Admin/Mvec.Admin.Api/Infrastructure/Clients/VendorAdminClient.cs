using Mvec.Admin.Api.Application.Abstractions;

namespace Mvec.Admin.Api.Infrastructure.Clients;

public sealed class VendorAdminClient : DownstreamClientBase, IVendorAdminClient
{
    public VendorAdminClient(HttpClient http) : base(http) { }

    public Task<int> GetPendingApprovalCountAsync(CancellationToken ct = default) =>
        ReadTotalAsync("api/vendors/pending?pageSize=1", ct);

    public Task<DownstreamResponse> ListPendingAsync(string? queryString, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Get, $"api/vendors/pending{queryString}", null, ct);

    public Task<DownstreamResponse> ListApprovedAsync(string? queryString, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Get, $"api/vendors/approved{queryString}", null, ct);

    public Task<DownstreamResponse> GetDetailAsync(long vendorId, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Get, $"api/vendors/{vendorId}", null, ct);

    public Task<DownstreamResponse> ApproveAsync(long vendorId, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Post, $"api/vendors/{vendorId}/approve", null, ct);

    public Task<DownstreamResponse> RejectAsync(long vendorId, string jsonBody, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Post, $"api/vendors/{vendorId}/reject", jsonBody, ct);

    public Task<DownstreamResponse> SuspendAsync(long vendorId, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Post, $"api/vendors/{vendorId}/suspend", null, ct);

    public Task<DownstreamResponse> ReinstateAsync(long vendorId, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Post, $"api/vendors/{vendorId}/reinstate", null, ct);
}
