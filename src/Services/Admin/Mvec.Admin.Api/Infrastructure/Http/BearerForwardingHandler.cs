namespace Mvec.Admin.Api.Infrastructure.Http;

/// <summary>
/// Copies the inbound request's <c>Authorization</c> header onto outgoing downstream calls, so the
/// admin's JWT flows through to Identity/Vendor (whose endpoints enforce the Admin policy themselves).
/// </summary>
public sealed class BearerForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public BearerForwardingHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var auth = _accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth) && request.Headers.Authorization is null)
            request.Headers.TryAddWithoutValidation("Authorization", auth);

        return base.SendAsync(request, ct);
    }
}
