namespace Jobsy.Core.Rules;

/// <summary>
/// Maps Quick-Scan workplace skills onto workshop search blobs and score-band copy keys.
/// Search text is skill language only — never a person name or contact detail.
/// </summary>
public static class CompetencyTrainingCatalog
{
    public const int HighMin = 70;
    public const int MidMin = 45;

    public static string SearchBlob(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => "samenwerken communicatie teamoverleg luisteren",
        CompetencyTestCatalog.Resultaatgerichtheid => "resultaatgericht deadlines organiseren afronden",
        CompetencyTestCatalog.Stressbestendigheid => "stressbestendig weerbaarheid werkdruk kalm",
        CompetencyTestCatalog.Innovatie => "innovatie probleemoplossen digitale vaardigheden",
        CompetencyTestCatalog.Extraversie => "klantcontact presenteren gastvrijheid verkoop",
        _ => category
    };

    public static string MeaningKey(string category, int score)
    {
        var band = score >= HighMin ? "High" : score >= MidMin ? "Mid" : "Low";
        return $"Competency.Cat.{category}.Meaning{band}";
    }
}
