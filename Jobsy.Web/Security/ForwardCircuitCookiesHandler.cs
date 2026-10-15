namespace Jobsy.Web.Security;

/// <summary>Forwards the browser cookie header from the Blazor circuit to same-site BFF calls.</summary>
public sealed class ForwardCircuitCookiesHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardCircuitCookiesHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.Request.Headers.Cookie is { Count: > 0 } cookies)
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookies.ToString());
        }

        if (http?.Request.Headers.TryGetValue("X-Jobsy-Language", out var lang) == true)
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Language", lang.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
