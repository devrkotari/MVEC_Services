using Mvec.Admin.Api.Application.Abstractions;

namespace Mvec.Admin.Api.Infrastructure.Clients;

public sealed class IdentityAdminClient : DownstreamClientBase, IIdentityAdminClient
{
    public IdentityAdminClient(HttpClient http) : base(http) { }

    public Task<int> GetUserCountAsync(CancellationToken ct = default) =>
        ReadTotalAsync("api/identity/users?pageSize=1", ct);

    public Task<DownstreamResponse> ListUsersAsync(string? queryString, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Get, $"api/identity/users{queryString}", null, ct);

    public Task<DownstreamResponse> ChangeUserStatusAsync(long userId, string jsonBody, CancellationToken ct = default) =>
        RelayAsync(HttpMethod.Patch, $"api/identity/users/{userId}/status", jsonBody, ct);
}
