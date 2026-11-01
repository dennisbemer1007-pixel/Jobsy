namespace Jobsy.Core.Scholen.Matching;

/// <summary>
/// Vak → ISCED-F 2013 koppeling met bron (ESCO/ISCED/eigen keuze). Algemene vakken tellen niet mee in de match.
/// </summary>
public static class PupilSubjectIscedCatalog
{
    public sealed record SubjectIscedRow(
        string SubjectKey,
        IReadOnlyList<string> IscedCodes,
        PupilSubjectIscedSource Source,
        bool CountsInMatch,
        IReadOnlyList<string>? LinkedOccupationJobKeys = null);

    private static readonly Dictionary<string, SubjectIscedRow> G78 =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["rekenen"] = Match("rekenen", PupilSubjectIscedSource.Isced, "0541"),
            ["aardrijkskunde"] = Match("aardrijkskunde", PupilSubjectIscedSource.Isced, "0532"),
            ["geschiedenis"] = Match("geschiedenis", PupilSubjectIscedSource.Isced, "0222"),
            ["natuur-techniek"] = Match("natuur-techniek", PupilSubjectIscedSource.Isced, "0713", "0714", "0715", "0533"),
            ["tekenen"] = Match(
                "tekenen",
                PupilSubjectIscedSource.EigenKeuze,
                ["0213", "0212"],
                ["grafisch-ontwerper"]),
            ["muziek"] = Match("muziek", PupilSubjectIscedSource.Isced, "0215"),
            ["gym"] = Match("gym", PupilSubjectIscedSource.Isced, "1014"),
            ["lezen"] = Skip("lezen"),
            ["spelling"] = Skip("spelling"),
            ["taal"] = Skip("taal"),
            ["engels"] = Skip("engels"),
            ["schrijven"] = Skip("schrijven"),
        };

    private static readonly Dictionary<string, SubjectIscedRow> MboHboVak =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["v01"] = Skip("v01"),
            ["v02"] = Skip("v02"),
            ["v03"] = Skip("v03"),
            ["v05"] = Skip("v05"),
            ["v28"] = Skip("v28"),
            ["v04"] = Match("v04", PupilSubjectIscedSource.Isced, "0541"),
            ["v06"] = Match("v06", PupilSubjectIscedSource.Isced, "0713", "0714"),
            ["v07"] = Match("v07", PupilSubjectIscedSource.Isced, "0715"),
            ["v08"] = Match("v08", PupilSubjectIscedSource.Isced, "0713"),
            ["v09"] = Match("v09", PupilSubjectIscedSource.Isced, "0732"),
            ["v10"] = Match("v10", PupilSubjectIscedSource.Isced, "0716"),
            ["v11"] = Match("v11", PupilSubjectIscedSource.Isced, "0613"),
            ["v12"] = Match("v12", PupilSubjectIscedSource.Isced, "0612"),
            ["v13"] = Match("v13", PupilSubjectIscedSource.Isced, "0611", "0612"),
            ["v14"] = Match("v14", PupilSubjectIscedSource.Isced, "0414", "0416"),
            ["v15"] = Match("v15", PupilSubjectIscedSource.Isced, "0413"),
            ["v16"] = Match("v16", PupilSubjectIscedSource.Isced, "0913"),
            ["v17"] = Match("v17", PupilSubjectIscedSource.Isced, "0923", "0313"),
            ["v18"] = Match("v18", PupilSubjectIscedSource.Isced, "0912"),
            ["v19"] = Match(
                "v19",
                PupilSubjectIscedSource.EigenKeuze,
                ["0213", "0212"],
                ["grafisch-ontwerper"]),
            ["v20"] = Match("v20", PupilSubjectIscedSource.Isced, "0215"),
            ["v21"] = Match("v21", PupilSubjectIscedSource.Isced, "0211"),
            ["v22"] = Match("v22", PupilSubjectIscedSource.Isced, "1012"),
            ["v23"] = Match("v23", PupilSubjectIscedSource.Isced, "1014"),
            ["v24"] = Match("v24", PupilSubjectIscedSource.Isced, "0841", "0812"),
            ["v25"] = Match("v25", PupilSubjectIscedSource.Isced, "0812", "0841"),
            ["v26"] = Match("v26", PupilSubjectIscedSource.Isced, "1013"),
            ["v27"] = Match("v27", PupilSubjectIscedSource.Isced, "1041"),
            ["v29"] = Match("v29", PupilSubjectIscedSource.Isced, "0415"),
            ["v30"] = Match("v30", PupilSubjectIscedSource.Isced, "0416"),
            ["v31"] = Match("v31", PupilSubjectIscedSource.Isced, "1013"),
            ["v32"] = Match("v32", PupilSubjectIscedSource.Isced, "1032"),
            ["v33"] = Match("v33", PupilSubjectIscedSource.Isced, "0922", "0111"),
            ["v34"] = Match("v34", PupilSubjectIscedSource.Isced, "1012"),
            ["v35"] = Match("v35", PupilSubjectIscedSource.Isced, "0715"),
            ["v36"] = Match("v36", PupilSubjectIscedSource.Isced, "0732"),
            ["v37"] = Match("v37", PupilSubjectIscedSource.Isced, "0812"),
            ["v38"] = Match("v38", PupilSubjectIscedSource.Isced, "0511"),
            ["v39"] = Match("v39", PupilSubjectIscedSource.Isced, "1011"),
            ["v40"] = Match("v40", PupilSubjectIscedSource.Isced, "0612", "0211"),
        };

    private static readonly Dictionary<string, SubjectIscedRow> VoFavorite =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["biologie"] = Match("biologie", PupilSubjectIscedSource.Isced, "0511"),
            ["natuurkunde"] = Match("natuurkunde", PupilSubjectIscedSource.Isced, "0533"),
            ["scheikunde"] = Match("scheikunde", PupilSubjectIscedSource.Isced, "0531"),
            ["wiskunde"] = Match("wiskunde", PupilSubjectIscedSource.Isced, "0541"),
            ["economie"] = Match("economie", PupilSubjectIscedSource.Isced, "0411"),
            ["tekenen"] = G78["tekenen"],
            ["muziek"] = G78["muziek"],
            ["gym"] = G78["gym"],
            ["geschiedenis"] = G78["geschiedenis"],
            ["aardrijkskunde"] = G78["aardrijkskunde"],
            ["nlt"] = G78["natuur-techniek"],
            ["natuur-techniek"] = G78["natuur-techniek"],
            ["engels"] = Skip("engels"),
            ["nederlands"] = Skip("nederlands"),
            ["frans"] = Skip("frans"),
            ["duits"] = Skip("duits"),
        };

    public static SubjectIscedRow? TryGetG78(string? subjectKey) => Try(G78, subjectKey);

    public static SubjectIscedRow? TryGetMboHboVak(string? vakItemId) => Try(MboHboVak, vakItemId);

    public static SubjectIscedRow? TryGetVoFavorite(string? subjectKey) => Try(VoFavorite, subjectKey);

    private static SubjectIscedRow? Try(IReadOnlyDictionary<string, SubjectIscedRow> map, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return map.TryGetValue(key.Trim(), out var row) ? row : null;
    }

    private static SubjectIscedRow Match(
        string key,
        PupilSubjectIscedSource source,
        params string[] codes)
        => new(key, codes, source, true);

    private static SubjectIscedRow Match(
        string key,
        PupilSubjectIscedSource source,
        IReadOnlyList<string> codes,
        IReadOnlyList<string> linkedJobs)
        => new(key, codes, source, true, linkedJobs);

    private static SubjectIscedRow Skip(string key)
        => new(key, [], PupilSubjectIscedSource.Isced, false);
}
