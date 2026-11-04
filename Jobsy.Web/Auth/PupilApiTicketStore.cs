using System.Collections.Concurrent;

namespace Jobsy.Web.Auth;

/// <summary>
/// Server-side copy of the opaque API pupil ticket, keyed by pupil code id.
/// The Blazor circuit has no request cookies, so <see cref="Jobsy.Web.Services.JobsyApiAuthHandler"/>
/// reads the ticket from here after <see cref="PupilCircuitAuthenticationMiddleware"/> captured it
/// on <c>/_blazor</c>. Never written into the staff cookie or the Web pupil cookie.
/// </summary>
public sealed class PupilApiTicketStore
{
    /// <summary>
    /// After a sliding refresh on this web node, the browser cookie can lag one hop.
    /// Prefer in-memory tickets newer than this window when they differ from the cookie.
    /// </summary>
    internal static readonly TimeSpan SameNodeBurstWindow = TimeSpan.FromSeconds(3);

    private readonly ConcurrentDictionary<string, PupilApiTicketEntry> _byCode = new(StringComparer.Ordinal);

    public void Set(string? pupilCodeId, string? ticket)
    {
        if (string.IsNullOrWhiteSpace(pupilCodeId) || string.IsNullOrWhiteSpace(ticket))
        {
            return;
        }

        _byCode[pupilCodeId] = new PupilApiTicketEntry(ticket, DateTimeOffset.UtcNow);
    }

    public string? Get(string? pupilCodeId)
        => TryGetEntry(pupilCodeId, out var entry) ? entry.Ticket : null;

    internal bool TryGetEntry(string? pupilCodeId, out PupilApiTicketEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(pupilCodeId))
        {
            return false;
        }

        return _byCode.TryGetValue(pupilCodeId, out entry);
    }

    /// <summary>
    /// Pick the API ticket to forward when the browser cookie and in-memory copy diverge.
    /// The browser cookie is authoritative when both exist: it survives load-balanced hops
    /// after sliding refresh on another web node. In-memory copies are per-instance only.
    /// </summary>
    internal string? ResolveTicket(string? pupilCodeId, string? browserCookie, TimeProvider? _ = null)
    {
        TryGetEntry(pupilCodeId, out var stored);
        var hasStored = !string.IsNullOrWhiteSpace(stored.Ticket);
        var hasCookie = !string.IsNullOrWhiteSpace(browserCookie);

        if (!hasStored)
        {
            return hasCookie ? browserCookie : null;
        }

        if (!hasCookie)
        {
            return stored.Ticket;
        }

        if (string.Equals(stored.Ticket, browserCookie, StringComparison.Ordinal))
        {
            return stored.Ticket;
        }

        return browserCookie;
    }

    public void Remove(string? pupilCodeId)
    {
        if (!string.IsNullOrWhiteSpace(pupilCodeId))
        {
            _byCode.TryRemove(pupilCodeId, out _);
        }
    }

    internal readonly record struct PupilApiTicketEntry(string Ticket, DateTimeOffset UpdatedAt);
}
