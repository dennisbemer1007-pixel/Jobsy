namespace Jobsy.Core.Scholen;

/// <summary>Child-friendly job-type clusters with fixed &quot;omdat&quot; copy (NL + EN, B1).</summary>
public static class PupilG78BeroepssoortCatalog
{
    public sealed record Entry(
        string Key,
        string TitleNl,
        string TitleEn,
        string OmdatNl,
        string OmdatEn);

    public static readonly IReadOnlyList<Entry> All =
    [
        new("techniek-maken", "Techniek en maken", "Technology and making",
            "Je bent graag bezig met bouwen, repareren of uitvinden.",
            "You like building, fixing or inventing things."),
        new("exact-wetenschap", "Uitzoeken en wetenschap", "Research and science",
            "Je wilt graag weten hoe dingen werken.",
            "You like finding out how things work."),
        new("creatief-design", "Creatief en ontwerpen", "Creative and design",
            "Je maakt graag iets moois of nieuws.",
            "You like making something beautiful or new."),
        new("taal-verhalen", "Taal en verhalen", "Language and stories",
            "Je leest, schrijft of vertelt graag.",
            "You like reading, writing or telling stories."),
        new("zorg-mensen", "Zorgen voor mensen", "Caring for people",
            "Je helpt anderen graag verder.",
            "You like helping other people."),
        new("natuur-buiten", "Natuur en buiten", "Nature and outdoors",
            "Buiten zijn geeft je energie.",
            "Being outdoors gives you energy."),
        new("sport-bewegen", "Sport en bewegen", "Sport and movement",
            "Je houdt van bewegen en samen spelen.",
            "You enjoy moving and playing together."),
        new("handel-organiseren", "Organiseren en regelen", "Organising and planning",
            "Je houdt van overzicht en duidelijke afspraken.",
            "You like clear plans and keeping things in order."),
        new("media-digital", "Media en digitaal", "Media and digital",
            "Computers, video of online dingen vind je leuk.",
            "You enjoy computers, video or online things."),
        new("dieren-leven", "Dieren en leven", "Animals and living things",
            "Dieren of planten interesseren je.",
            "Animals or plants interest you."),
        new("veiligheid-hulp", "Veiligheid en hulp", "Safety and helping",
            "Je wilt anderen beschermen of geruststellen.",
            "You want to protect or reassure others."),
        new("ondernemen-leiden", "Ondernemen en leiden", "Business and leading",
            "Je bedenkt graag plannen en neemt het voortouw.",
            "You like making plans and taking the lead."),
    ];

    public static Entry? TryGet(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return All.FirstOrDefault(e =>
            string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public static string Title(string key, bool english)
    {
        var e = TryGet(key);
        if (e is null)
        {
            return key;
        }

        return english ? e.TitleEn : e.TitleNl;
    }

    public static string Omdat(string key, bool english)
    {
        var e = TryGet(key);
        if (e is null)
        {
            return "";
        }

        return english ? e.OmdatEn : e.OmdatNl;
    }
}
