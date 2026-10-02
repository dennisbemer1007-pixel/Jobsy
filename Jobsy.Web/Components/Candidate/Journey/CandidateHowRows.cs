using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Journey;

/// <summary>One rendered row: the stone plus its read-only state (05 §2).</summary>
public sealed record CandidateHowRow(
    CandidateHowStone Stone,
    int Number,
    bool Done,
    bool IsNow);

/// <summary>Row states derived from <c>GET api/me/journey-summary</c>. Pure; never writes.</summary>
public static class CandidateHowRows
{
    /// <summary>
    /// Marks each stone done from the summary and puts <c>now</c> on the first stone that is not
    /// done. When every stone is done there is no <c>now</c> row and the last stone is the target.
    /// </summary>
    public static IReadOnlyList<CandidateHowRow> Build(
        IReadOnlyList<CandidateHowStone> stones,
        CandidateJourneySummaryApiModel? summary)
    {
        var nowIndex = -1;
        for (var i = 0; i < stones.Count; i++)
        {
            if (!IsDone(stones[i].Kind, summary))
            {
                nowIndex = i;
                break;
            }
        }

        var rows = new List<CandidateHowRow>(stones.Count);
        for (var i = 0; i < stones.Count; i++)
        {
            rows.Add(new CandidateHowRow(
                stones[i],
                i + 1,
                IsDone(stones[i].Kind, summary),
                i == nowIndex));
        }

        return rows;
    }

    public static int DoneCount(IReadOnlyList<CandidateHowRow> rows)
        => rows.Count(r => r.Done);

    /// <summary>The stone the primary button walks to: the <c>now</c> row, else the last stone.</summary>
    public static CandidateHowRow? Target(IReadOnlyList<CandidateHowRow> rows)
        => rows.FirstOrDefault(r => r.IsNow) ?? rows.LastOrDefault();

    private static bool IsDone(CandidateHowStoneKind kind, CandidateJourneySummaryApiModel? summary)
    {
        if (summary is null)
        {
            return false;
        }

        return kind switch
        {
            CandidateHowStoneKind.Discovery => summary.DiscoveryDone,
            CandidateHowStoneKind.Passport => summary.PassportDone,
            CandidateHowStoneKind.Career => summary.CareerDone,
            CandidateHowStoneKind.JobMap => summary.JobMapDone,
            CandidateHowStoneKind.Applications => summary.ApplicationsDone,
            _ => false
        };
    }
}
