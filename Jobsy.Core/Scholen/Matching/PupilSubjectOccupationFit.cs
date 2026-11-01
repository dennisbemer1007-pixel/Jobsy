namespace Jobsy.Core.Scholen.Matching;

public static class PupilSubjectOccupationFit
{
    public static double FitForDreamJob(
        string jobKey,
        IReadOnlyList<PupilVakScoreEntry> g78VakScores)
        => Fit(
            PupilDreamJobEscoCatalog.IscedCodesForJob(jobKey),
            jobKey,
            g78VakScores.Select(v => (v.SubjectKey, PupilVakScores.SubjectScore(v.Leuk, v.Goed), PupilSubjectIscedCatalog.TryGetG78(v.SubjectKey))).ToList());

    public static double FitForMboHboOccupation(
        string? escoUri,
        IReadOnlyList<(string VakItemId, double Score)> vakScores)
    {
        var occCodes = PupilEscoIscedIndex.IscedCodesForUri(escoUri);
        if (occCodes.Count == 0)
        {
            return PupilCareerMatchWeights.NeutralSubjectFit;
        }

        var rows = vakScores
            .Select(v => (v.VakItemId, v.Score, PupilSubjectIscedCatalog.TryGetMboHboVak(v.VakItemId)))
            .ToList();
        return Fit(occCodes, jobKey: null, rows);
    }

    public static double FitForVoFavorites(
        string jobKey,
        IReadOnlyList<string> favoriteSubjectKeys)
    {
        var occCodes = PupilDreamJobEscoCatalog.IscedCodesForJob(jobKey);
        if (occCodes.Count == 0)
        {
            return PupilCareerMatchWeights.NeutralSubjectFit;
        }

        var rows = favoriteSubjectKeys
            .Select(k => (SubjectKey: k, Score: 0.85, Row: PupilSubjectIscedCatalog.TryGetVoFavorite(k)))
            .Where(x => x.Row is not null)
            .Select(x => (x.SubjectKey, x.Score, x.Row))
            .ToList();
        return Fit(occCodes, jobKey, rows);
    }

    private static double Fit(
        IReadOnlyList<string> occupationIscedCodes,
        string? jobKey,
        IReadOnlyList<(string SubjectKey, double Score, PupilSubjectIscedCatalog.SubjectIscedRow? Row)> rows)
    {
        if (occupationIscedCodes.Count == 0)
        {
            return PupilCareerMatchWeights.NeutralSubjectFit;
        }

        var occSet = new HashSet<string>(occupationIscedCodes, StringComparer.OrdinalIgnoreCase);
        var sum = 0.0;
        var n = 0;
        foreach (var (subjectKey, score, row) in rows)
        {
            if (row is null || !row.CountsInMatch)
            {
                continue;
            }

            var linked = row.LinkedOccupationJobKeys is not null
                         && jobKey is not null
                         && row.LinkedOccupationJobKeys.Any(j =>
                             string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase));
            var iscedHit = row.IscedCodes.Any(c => occSet.Contains(c));
            if (!linked && !iscedHit)
            {
                continue;
            }

            sum += score;
            n++;
        }

        return n == 0 ? PupilCareerMatchWeights.NeutralSubjectFit : Math.Clamp(sum / n, 0, 1);
    }
}
