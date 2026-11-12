namespace Jobsy.Core.Scholen;

/// <summary>Flowing Zo/En/Ook example lines per competence item (label-aligned).</summary>
public static class PupilG78CompetencyZoForms
{
    private static readonly Dictionary<int, string> Stems = new()
    {
        [9001] = "als een vriend verdrietig is, merk je dat snel",
        [9002] = "maak je je huiswerk af, ook als je geen zin hebt",
        [9003] = "als je een slecht cijfer terugkrijgt, blijf je rustig",
        [9004] = "bedenk je graag een nieuwe manier om iets te doen",
        [9005] = "praat je in een nieuwe klas snel met anderen",
        [9006] = "help je graag een klasgenoot die iets niet snapt",
        [9007] = "maak je een plan voordat je aan een klus begint",
        [9008] = "bij een lastige toets blijf je nadenken zonder paniek",
        [9009] = "vind je het leuk om iets nieuws te proberen",
        [9010] = "voel je je op je gemak in een drukke groep",
    };

    public static bool TryPhrase(int itemId, int ordinal, out string phrase)
    {
        phrase = "";
        if (!Stems.TryGetValue(itemId, out var stem) || stem.Length == 0)
        {
            return false;
        }

        if (stem.StartsWith("als ", StringComparison.OrdinalIgnoreCase))
        {
            phrase = ordinal switch
            {
                0 => char.ToUpperInvariant(stem[0]) + stem[1..] + ".",
                1 => "En " + stem + ".",
                _ => "Ook " + stem + "."
            };
            return true;
        }

        phrase = ordinal switch
        {
            0 => "Zo " + stem + ".",
            1 => "En " + stem + ".",
            _ => "Ook " + stem + "."
        };
        return true;
    }
}
