namespace Jobsy.Core.Rules;

/// <summary>Maps DISC workplace styles onto workshop search text (no person data).</summary>
public static class DiscTrainingCatalog
{
    public const int HighMin = 70;
    public const int MidMin = 45;

    public static string SearchBlob(string category) => category switch
    {
        DiscTestCatalog.Dominant => "leiding nemen besluiten tempo aanpakken",
        DiscTestCatalog.Invloed => "communicatie presenteren klantcontact overtuigen",
        DiscTestCatalog.Stabiel => "samenwerken ritme rust teamoverleg",
        DiscTestCatalog.Nauwkeurig => "nauwkeurig kwaliteit administratie checklists",
        _ => category
    };

    public static string MeaningKey(string category, int score)
    {
        var band = score >= HighMin ? "High" : score >= MidMin ? "Mid" : "Low";
        return $"Disc.Cat.{category}.Meaning{band}";
    }

    public static string DevelopKey(string category) => $"Disc.Cat.{category}.Develop";
}
