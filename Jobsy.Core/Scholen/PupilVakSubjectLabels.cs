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
}
