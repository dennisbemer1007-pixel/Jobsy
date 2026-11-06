namespace Jobsy.Web.Auth;

/// <summary>
/// Per-request copy of the opaque API pupil ticket. Load-balanced web nodes must not
/// share ticket state in memory; the browser cookie (and refreshes on the same HTTP
/// request) are the only source of truth across requests.
/// </summary>
public sealed class PupilRequestApiTicket
{
    private string? _current;
    private bool _loadedFromRequest;

    /// <summary>Ticket to forward on this request (cookie first, then in-request refresh).</summary>
    public string? GetForForwarding(HttpContext? http)
    {
        if (!string.IsNullOrWhiteSpace(_current))
        {
            return _current;
        }

        if (http is null)
        {
            return null;
        }

        if (!_loadedFromRequest)
        {
            _current = PupilApiSessionCookie.Read(http);
            _loadedFromRequest = true;
        }

        return _current;
    }

    /// <summary>Apply sliding refresh from the API on this same request.</summary>
    public void ApplyRefresh(string ticket, HttpContext? http)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return;
        }

        _current = ticket;
        _loadedFromRequest = true;
        if (http is not null && !http.Response.HasStarted)
        {
            PupilApiSessionCookie.Set(http, ticket);
        }
    }

    public void Clear() => _current = null;
}
