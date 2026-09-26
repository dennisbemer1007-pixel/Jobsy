namespace Jobsy.Web.Services;

/// <summary>
/// Counts outbound API requests for <see cref="ApiCallTracker"/> (Development logging).
/// </summary>
public sealed class ApiCallTrackingHandler : DelegatingHandler
{
    private readonly ApiCallTracker? _tracker;

    public ApiCallTrackingHandler(ApiCallTracker? tracker)
    {
        _tracker = tracker;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _tracker?.Record(request.RequestUri);
        return base.SendAsync(request, cancellationToken);
    }
}
