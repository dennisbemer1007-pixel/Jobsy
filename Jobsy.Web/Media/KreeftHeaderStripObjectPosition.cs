namespace Jobsy.Web.Media;

/// <summary>object-position per strip key (mockup kreeft v4 — kreeft altijd in crop).</summary>
public static class KreeftHeaderStripObjectPosition
{
    public static string ForKey(string stripKey)
    {
        if (string.IsNullOrWhiteSpace(stripKey))
        {
            return "center center";
        }

        return stripKey.Trim().ToLowerInvariant() switch
        {
            "landing" => "68% 52%",
            "inloggen" => "62% 48%",
            "route-kaart" => "58% 50%",
            "licht-kompas" => "50% 42%",
            "licht-paspoort" => "58% 46%",
            "banenkaart" or "bedrijvenkaart" => "52% 44%",
            "sollicitaties" or "berichten" or "activiteiten" or "na-de-match" or "moment" => "60% 46%",
            "carriere" or "omscholen" or "wat-drijft" => "54% 44%",
            "bewaard" => "58% 46%",
            "profiel" or "instellingen" => "56% 48%",
            "nu-niet" => "50% 50%",
            _ when stripKey.StartsWith("kust-", StringComparison.Ordinal) => "65% 48%",
            _ when stripKey.StartsWith("rotsen-", StringComparison.Ordinal) => "62% 46%",
            _ when stripKey.StartsWith("diepte-", StringComparison.Ordinal) => "58% 50%",
            _ => "55% 48%",
        };
    }
}
