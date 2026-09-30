using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Match;

public static class TopMatchTileVisibility
{
    public static bool ShouldShow(bool isWide, bool isCandidate, bool gateComplete, int count)
        => isWide && isCandidate && gateComplete && count > 0;

    /// <summary>
    /// Candidate strong-fit tile (≥ <see cref="CandidateFitDisplay.StrongThreshold"/>), D2.
    /// </summary>
    public static bool ShouldShowStrong(
        bool isWide,
        bool isCandidate,
        bool profileComplete,
        bool fitGateOpen,
        SwipeViewModel? current)
    {
        if (!ShouldShow(isWide, isCandidate, profileComplete && fitGateOpen, current is null ? 0 : 1))
        {
            return false;
        }

        return current?.MatchPercentage is int pct && pct >= CandidateFitDisplay.StrongThreshold;
    }
}
