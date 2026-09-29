namespace Jobsy.Web.Navigation;

/// <summary>
/// Tab ids for Mijn Paspoort (<c>/candidate/paspoort?tab=</c>).
/// Legacy Kompas / bookmark values map via <see cref="Normalize"/>.
/// </summary>
public static class PassportTabs
{
    public const string Dna = "dna";
    public const string Tests = "tests";
    public const string Fit = "fit";
    public const string Career = "career";
    public const string Proof = "proof";
    public const string Data = "data";

    public static readonly string[] All = [Dna, Tests, Fit, Career, Proof, Data];

    public static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim().TrimStart('#').ToLowerInvariant();
        var slash = value.LastIndexOf('/');
        if (slash >= 0 && slash < value.Length - 1)
        {
            value = value[(slash + 1)..];
        }

        return value switch
        {
            Dna or "wie-ben-ik" or "wie ben ik" or "whoami" or "who-am-i" or "who am i"
                or "mijn-dna" or "mijn dna"
                => Dna,
            Tests or "competenties" or "competences" or "mijn-tests" or "mijn tests"
                => Tests,
            Fit or "functiefit" or "role-fit" or "past-dit" or "past dit bij mij"
                => Fit,
            Career or "carriere" or "carrière" or "loopbaan"
                => Career,
            Proof or "bewijzen" or "bewijs"
                => Proof,
            Data or "gegevens" or "profile" or "profiel" or "mijn-gegevens" or "mijn gegevens"
                => Data,
            _ => Dna
        };
    }

    public static string Neighbor(string current, int delta)
    {
        var index = Array.IndexOf(All, Normalize(current));
        if (index < 0)
        {
            index = 0;
        }

        var next = (index + delta) % All.Length;
        if (next < 0)
        {
            next += All.Length;
        }

        return All[next];
    }

    /// <summary>Maps a passport tab to the classic Kompas tab id.</summary>
    public static string ToKompasTab(string passportTab)
        => Normalize(passportTab) switch
        {
            Data => CandidateKompasTabs.Profile,
            Tests => CandidateKompasTabs.Tests,
            Fit => CandidateKompasTabs.Fit,
            _ => CandidateKompasTabs.Dna
        };

    /// <summary>Maps a classic Kompas tab id to a passport tab.</summary>
    public static string FromKompasTab(string? kompasTab)
    {
        var k = CandidateKompasTabs.Normalize(kompasTab);
        return k switch
        {
            CandidateKompasTabs.Profile => Data,
            CandidateKompasTabs.Tests => Tests,
            CandidateKompasTabs.Fit => Fit,
            _ => Dna
        };
    }
}
