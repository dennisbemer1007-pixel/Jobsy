namespace Jobsy.Core.Rules;

/// <summary>Maps culture/personality facets onto workshop search text (no person data).</summary>
public static class CultureTrainingCatalog
{
    public const int HighMin = 70;
    public const int MidMin = 45;

    public static string SearchBlob(string category) => category switch
    {
        CulturePersonalityCatalog.Autonomy => "zelfstandig werken initiatief nemen",
        CulturePersonalityCatalog.Informal => "informeel team sfeer communicatie",
        CulturePersonalityCatalog.Collaboration => "samenwerken teamoverleg",
        CulturePersonalityCatalog.Flexibility => "flexibel meebewegen veranderingen",
        CulturePersonalityCatalog.Innovation => "innovatie verbeteren vernieuwen",
        CulturePersonalityCatalog.PeopleFirst => "mensgericht klantcontact coaching",
        CulturePersonalityCatalog.Openness => "leren nieuwsgierig openstaan",
        CulturePersonalityCatalog.Conscientiousness => "nauwkeurig kwaliteit administratie",
        CulturePersonalityCatalog.Extraversion => "klantcontact presenteren netwerken",
        CulturePersonalityCatalog.Agreeableness => "samenwerken conflictbemiddeling",
        CulturePersonalityCatalog.EmotionalStability => "stressbestendig kalm onder druk",
        _ => category
    };

    public static string MeaningKey(string category, int score)
    {
        var band = score >= HighMin ? "High" : score >= MidMin ? "Mid" : "Low";
        return $"CultureScan.Cat.{category}.Meaning{band}";
    }

    public static string DevelopKey(string category) => $"CultureScan.Cat.{category}.Develop";
}
