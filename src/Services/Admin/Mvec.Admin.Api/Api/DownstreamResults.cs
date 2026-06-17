using Microsoft.AspNetCore.Mvc;
using Mvec.Admin.Api.Application.Abstractions;

namespace Mvec.Admin.Api.Api;

/// <summary>Relays a downstream service response (status + JSON body) back to the caller verbatim.</summary>
public static class DownstreamResults
{
    public static IActionResult Relay(this DownstreamResponse response) => new ContentResult
    {
        StatusCode = response.StatusCode,
        Content = response.Content,
        ContentType = "application/json"
    };
}
