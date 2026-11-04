using Jobsy.Web.Auth;

namespace Jobsy.Tests.Scholen;

public class PupilApiTicketStoreTests
{
    private const string CodeId = "11111111-2222-3333-4444-555555555555";

    [Fact]
    public void ResolveTicket_prefers_fresh_browser_cookie_when_memory_is_stale()
    {
        var store = new PupilApiTicketStore();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-11-04T18:00:00Z"));
        store.Set(CodeId, "stale-ticket");
        clock.Advance(PupilApiTicketStore.SameNodeBurstWindow + TimeSpan.FromSeconds(1));

        var resolved = store.ResolveTicket(CodeId, "fresh-ticket", clock);
        Assert.Equal("fresh-ticket", resolved);
    }

    [Fact]
    public void ResolveTicket_prefers_browser_cookie_when_tickets_differ_even_within_burst_window()
    {
        var store = new PupilApiTicketStore();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-11-04T18:00:00Z"));
        store.Set(CodeId, "older-node-ticket");
        clock.Advance(TimeSpan.FromMilliseconds(500));

        var resolved = store.ResolveTicket(CodeId, "newer-browser-ticket", clock);
        Assert.Equal("newer-browser-ticket", resolved);
    }

    private sealed class FakeClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
