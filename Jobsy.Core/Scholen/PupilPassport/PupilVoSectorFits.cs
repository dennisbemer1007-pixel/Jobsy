namespace Jobsy.Core.Scholen.PupilPassport;

public static class PupilVoSectorFits
{
    private sealed class SectorDef
    {
        public required string Key { get; init; }
        public required Func<string?, IReadOnlyList<string>, int> Score { get; init; }
    }

    private static readonly SectorDef[] Catalog =
    [
        new() { Key = "dieren-natuur", Score = (h, likes) =>
        {
            var s = 0;
            if (likes.Any(l => l is "dieren" or "natuur" or "buiten")) s += 4;
            if (h?.Contains('S', StringComparison.Ordinal) == true) s += 1;
            if (h?.Contains('R', StringComparison.Ordinal) == true) s += 1;
            return s;
        }},
        new() { Key = "zorg", Score = (h, likes) =>
        {
            var s = 0;
            if (h?.Contains('S', StringComparison.Ordinal) == true) s += 3;
            if (likes.Any(l => l is "kleine-kinderen" or "dieren")) s += 2;
            return s;
        }},
        new() { Key = "lab", Score = (h, likes) =>
        {
            var s = 0;
            if (h?.Contains('I', StringComparison.Ordinal) == true) s += 3;
            if (likes.Any(l => l is "puzzels" or "techniek" or "natuur")) s += 1;
            return s;
        }},
        new() { Key = "techniek", Score = (h, likes) =>
        {
            var s = 0;
            if (h?.Contains('R', StringComparison.Ordinal) == true) s += 3;
            if (likes.Any(l => l is "techniek" or "bouwen" or "sport")) s += 2;
            return s;
        }},
        new() { Key = "creatief", Score = (h, likes) =>
        {
            var s = 0;
            if (h?.Contains('A', StringComparison.Ordinal) == true) s += 3;
            if (likes.Any(l => l is "tekenen" or "muziek")) s += 2;
            return s;
        }},
        new() { Key = "handel", Score = (h, _) => h?.Contains('E', StringComparison.Ordinal) == true ? 3 : 0 },
        new() { Key = "admin", Score = (h, likes) =>
        {
            var s = h?.Contains('C', StringComparison.Ordinal) == true ? 3 : 0;
            if (likes.Any(l => l is "puzzels" or "lezen")) s += 1;
            return s;
        }},
    ];

    public static IReadOnlyList<PupilPassportSectorFit> Build(
        string? hollandCode,
        IReadOnlyList<string> likeKeys)
    {
        var ranked = Catalog
            .Select(def => (def, score: def.Score(hollandCode, likeKeys)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.def.Key, StringComparer.Ordinal)
            .Take(3)
            .ToList();

        if (ranked.Count < 3)
        {
            foreach (var def in Catalog)
            {
                if (ranked.Any(r => r.def.Key == def.Key))
                {
                    continue;
                }

                ranked.Add((def, 1));
                if (ranked.Count == 3)
                {
                    break;
                }
            }
        }

        var subtitles = new[] { "Mijn eerste keus", "Ook interessant", "Wil ik verkennen" };
        var fits = new List<PupilPassportSectorFit>();
        for (var i = 0; i < ranked.Count; i++)
        {
            var key = ranked[i].def.Key;
            fits.Add(new PupilPassportSectorFit(
                i + 1,
                PupilPassportCopy.Get($"LeerlingPassport.Vo.Sector.{key}.Title"),
                subtitles[i],
                Bullets(key),
                []));
        }

        return fits;
    }

    private static IReadOnlyList<string> Bullets(string sectorKey)
    {
        var list = new List<string>();
        for (var n = 1; n <= 3; n++)
        {
            var k = $"LeerlingPassport.Vo.Sector.{sectorKey}.B{n}";
            var text = PupilPassportCopy.Get(k);
            if (!text.StartsWith("LeerlingPassport.", StringComparison.Ordinal))
            {
                list.Add(text);
            }
        }

        return list;
    }
}
