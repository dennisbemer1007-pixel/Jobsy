namespace Jobsy.Core.Rules;

/// <summary>Plain-language papers and courses that often sit under a occupation title.</summary>
public static class CareerOccupationDetail
{
    public static IReadOnlyList<string> Requirements(string title)
    {
        var folded = CareerOccupationKeys.Fold(title);
        var items = new List<string>();
        if (folded.Contains("verpleeg", StringComparison.Ordinal))
        {
            items.Add("Diploma verpleegkunde");
            items.Add("BIG-registratie");
        }
        else if (folded.Contains("helpende", StringComparison.Ordinal) || folded.Contains("zorg", StringComparison.Ordinal))
        {
            items.Add("Helpende- of verzorgende opleiding (vaak BBL)");
        }

        if (folded.Contains("monteur", StringComparison.Ordinal) || folded.Contains("elektricien", StringComparison.Ordinal))
        {
            items.Add("VCA");
            items.Add("Vakdiploma of BBL-leerwerktraject");
        }

        if (folded.Contains("piloot", StringComparison.Ordinal) || folded.Contains("vlieg", StringComparison.Ordinal))
        {
            items.Add("Vliegbrevet");
        }

        if (folded.Contains("juf", StringComparison.Ordinal)
            || folded.Contains("meester", StringComparison.Ordinal)
            || folded.Contains("docent", StringComparison.Ordinal)
            || folded.Contains("leraar", StringComparison.Ordinal))
        {
            items.Add("Pabo of pedagogische aantekening");
        }

        if (folded.Contains("onderwijsassistent", StringComparison.Ordinal)
            || folded.Contains("pedagogisch", StringComparison.Ordinal))
        {
            items.Add("MBO pedagogisch werk of onderwijsassistent");
        }

        if (folded.Contains("heftruck", StringComparison.Ordinal) || folded.Contains("magazijn", StringComparison.Ordinal))
        {
            items.Add("Heftruckcertificaat (indien de vacature dat vraagt)");
        }

        if (items.Count == 0)
        {
            items.Add("Geen extra diploma verplicht — reistijd en beschikbaarheid zijn vaak genoeg.");
        }

        return items;
    }
}
