namespace Jobsy.Core.Scholen;

/// <summary>Fixed RIASEC weights and beroepssoort links per G78 subject (deterministic scoring).</summary>
public static class PupilSubjectMap
{
    public sealed record SubjectDef(
        string Key,
        IReadOnlyDictionary<char, double> RiasecWeights,
        IReadOnlyList<string> BeroepssoortKeys);

    public static readonly IReadOnlyDictionary<string, SubjectDef> All =
        new Dictionary<string, SubjectDef>(StringComparer.OrdinalIgnoreCase)
        {
            ["rekenen"] = Def("rekenen", Ri(0.35, 0.25, 0.05, 0.05, 0.05, 0.25),
                "techniek-maken", "exact-wetenschap", "handel-organiseren"),
            ["lezen"] = Def("lezen", Ri(0.05, 0.25, 0.2, 0.25, 0.05, 0.2),
                "taal-verhalen", "zorg-mensen", "creatief-design"),
            ["spelling"] = Def("spelling", Ri(0.05, 0.2, 0.15, 0.1, 0.05, 0.45),
                "taal-verhalen", "handel-organiseren"),
            ["taal"] = Def("taal", Ri(0.05, 0.15, 0.25, 0.3, 0.15, 0.1),
                "taal-verhalen", "zorg-mensen", "creatief-design"),
            ["engels"] = Def("engels", Ri(0.05, 0.15, 0.25, 0.2, 0.25, 0.1),
                "taal-verhalen", "handel-organiseren", "creatief-design"),
            ["schrijven"] = Def("schrijven", Ri(0.05, 0.2, 0.35, 0.15, 0.1, 0.15),
                "taal-verhalen", "creatief-design"),
            ["aardrijkskunde"] = Def("aardrijkskunde", Ri(0.2, 0.3, 0.05, 0.25, 0.05, 0.15),
                "natuur-buiten", "exact-wetenschap", "zorg-mensen"),
            ["geschiedenis"] = Def("geschiedenis", Ri(0.05, 0.25, 0.2, 0.3, 0.05, 0.15),
                "taal-verhalen", "zorg-mensen", "handel-organiseren"),
            ["natuur-techniek"] = Def("natuur-techniek", Ri(0.35, 0.4, 0.05, 0.05, 0.05, 0.1),
                "techniek-maken", "exact-wetenschap", "natuur-buiten"),
            ["tekenen"] = Def("tekenen", Ri(0.25, 0.15, 0.45, 0.05, 0.05, 0.05),
                "creatief-design", "techniek-maken"),
            ["muziek"] = Def("muziek", Ri(0.1, 0.05, 0.5, 0.25, 0.05, 0.05),
                "creatief-design", "zorg-mensen"),
            ["gym"] = Def("gym", Ri(0.35, 0.05, 0.05, 0.3, 0.2, 0.05),
                "sport-bewegen", "zorg-mensen", "natuur-buiten"),
        };

    public static SubjectDef? TryGet(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return All.TryGetValue(key.Trim(), out var def) ? def : null;
    }

    private static SubjectDef Def(string key, IReadOnlyDictionary<char, double> weights, params string[] beroepen)
        => new(key, weights, beroepen);

    private static IReadOnlyDictionary<char, double> Ri(double r, double i, double a, double s, double e, double c)
        => new Dictionary<char, double>
        {
            ['R'] = r,
            ['I'] = i,
            ['A'] = a,
            ['S'] = s,
            ['E'] = e,
            ['C'] = c,
        };
}
