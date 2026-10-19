namespace Jobsy.Core.Uwv;

public static class UwvAdviseurSectionKeys
{
    public const string Career = "career";
    public const string Kansberoepen = "kansberoepen";
    public const string Drijfveren = "drijfveren";
    public const string Competences = "competences";
    public const string Banenkaart = "banenkaart";

    public static IReadOnlyList<string> All { get; } =
    [
        Career,
        Kansberoepen,
        Drijfveren,
        Competences,
        Banenkaart
    ];

    public static bool IsValid(string key)
        => All.Contains(key, StringComparer.Ordinal);
}
