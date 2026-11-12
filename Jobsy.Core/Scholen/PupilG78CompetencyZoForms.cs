namespace Jobsy.Core.Scholen;

/// <summary>Flowing Zo/En/Ook example lines per competence item (label-aligned).</summary>
public static class PupilG78CompetencyZoForms
{
    private static readonly Dictionary<int, string> Stems = new()
    {
        [9001] = "merk je snel als een vriend verdrietig is",
        [9002] = "maak je je huiswerk af, ook als je geen zin hebt",
        [9003] = "blijf je rustig als je een slecht cijfer terugkrijgt",
        [9004] = "bedenk je graag een nieuwe manier om iets te doen",
        [9005] = "praat je in een nieuwe klas snel met anderen",
        [9006] = "help je graag een klasgenoot die iets niet snapt",
        [9007] = "maak je een plan voordat je aan een klus begint",
        [9008] = "bij een lastige toets blijf je nadenken zonder paniek",
        [9009] = "vind je het leuk om iets nieuws te proberen",
        [9010] = "voel je op je gemak in een drukke groep",
    };

    public static bool TryStem(int itemId, out string stem)
        => Stems.TryGetValue(itemId, out stem!) && stem.Length > 0;

    /// <summary>Joins 1–3 stems: Zo … en … ; at most one leading Ook.</summary>
    public static string JoinExampleStems(IReadOnlyList<string> stems)
    {
        if (stems.Count == 0)
        {
            return "";
        }

        if (stems.Count == 1)
        {
            return "Zo " + stems[0] + ".";
        }

        if (stems.Count == 2)
        {
            return "Zo " + stems[0] + " en " + stems[1] + ".";
        }

        return "Zo " + stems[0] + " en " + stems[1] + ". Ook " + stems[2] + ".";
    }

}
