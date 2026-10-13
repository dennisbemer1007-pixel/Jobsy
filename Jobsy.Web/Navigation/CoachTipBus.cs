namespace Jobsy.Web.Navigation;

/// <summary>
/// Page tip for the one candidate coach. Pages publish the sentence that used to
/// live in <c>LobsyBubble</c>. The coach shows it only when the route still matches.
/// </summary>
public sealed class CoachTipBus
{
    public string? Route { get; private set; }

    public string? Tip { get; private set; }

    public event Action? Changed;

    public void Publish(string? route, string? tip)
    {
        var nextRoute = string.IsNullOrWhiteSpace(route) ? "" : route.Trim();
        var nextTip = string.IsNullOrWhiteSpace(tip) ? null : tip.Trim();
        if (string.Equals(Route, nextRoute, StringComparison.Ordinal)
            && string.Equals(Tip, nextTip, StringComparison.Ordinal))
        {
            return;
        }

        Route = nextRoute;
        Tip = nextTip;
        Changed?.Invoke();
    }
}
