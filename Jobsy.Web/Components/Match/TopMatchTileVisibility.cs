namespace Jobsy.Web.Components.Match;

public static class TopMatchTileVisibility
{
    public static bool ShouldShow(bool isWide, bool isCandidate, bool gateComplete, int count)
        => isWide && isCandidate && gateComplete && count > 0;
}
