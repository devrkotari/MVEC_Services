using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Mvec.Admin.Api.Application.Abstractions;

namespace Mvec.Admin.Api.Infrastructure.Clients;

/// <summary>Shared HTTP plumbing for the downstream admin clients: relay calls and count reads.</summary>
public abstract class DownstreamClientBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected HttpClient Http { get; }

    protected DownstreamClientBase(HttpClient http) => Http = http;

    /// <summary>Sends a request and relays the downstream status + raw JSON body back verbatim.</summary>
    protected async Task<DownstreamResponse> RelayAsync(HttpMethod method, string path,
        string? jsonBody, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (jsonBody is not null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        return new DownstreamResponse((int)response.StatusCode, content);
    }

    /// <summary>Reads <c>PagedResult.Total</c> from a paged list endpoint.</summary>
    protected async Task<int> ReadTotalAsync(string path, CancellationToken ct)
    {
        var envelope = await Http.GetFromJsonAsync<PagedEnvelope>(path, Json, ct);
        return envelope?.Total ?? 0;
    }

    private sealed record PagedEnvelope(int Total);
}
