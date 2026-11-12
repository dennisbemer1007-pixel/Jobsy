namespace Jobsy.Core.Scholen;

public static class PupilVakSubjectLabels
{
    public static string Nl(string subjectKey) => subjectKey.ToLowerInvariant() switch
    {
        "rekenen" => "Rekenen",
        "lezen" => "Lezen",
        "spelling" => "Spelling",
        "taal" => "Taal",
        "engels" => "Engels",
        "schrijven" => "Schrijven",
        "aardrijkskunde" => "Aardrijkskunde",
        "geschiedenis" => "Geschiedenis",
        "natuur-techniek" => "Natuur & techniek",
        "tekenen" => "Tekenen/handvaardigheid",
        "muziek" => "Muziek",
        "gym" => "Gym",
        _ => subjectKey
    };

    public static string UiKey(string subjectKey) => $"Leerling.Vakken.Subject.{subjectKey}";

    /// <summary>Running text in stories: proper names capitalized, joined with &quot;en&quot;.</summary>
    public static string NlStoryList(string subjectKey) => subjectKey.ToLowerInvariant() switch
    {
        "engels" => "Engels",
        "frans" => "Frans",
        "geschiedenis" => "geschiedenis",
        "rekenen" => "rekenen",
        "natuur-techniek" => "natuur en techniek",
        "tekenen" => "tekenen en handvaardigheid",
        "muziek" => "muziek",
        "lezen" => "lezen",
        _ => subjectKey.ToLowerInvariant()
    };
}
