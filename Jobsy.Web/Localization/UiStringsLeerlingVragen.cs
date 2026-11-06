namespace Jobsy.Web.Localization;

/// <summary>
/// Dutch-only leerling question bank strings (D12). Prefix LeerlingQ. — reviewable in one file.
/// </summary>
public static class UiStringsLeerlingVragen
{
    public static void MergeNl(Dictionary<string, string> nl)
        => Jobsy.Core.Scholen.PupilQuestionBankNlStrings.MergeInto(nl);
}
