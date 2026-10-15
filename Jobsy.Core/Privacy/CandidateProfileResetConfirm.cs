namespace Jobsy.Core.Privacy;

/// <summary>Typed confirmation for irreversible candidate profile reset (i18n).</summary>
public static class CandidateProfileResetConfirm
{
    public static string ExpectedPhrase(string? language)
        => language?.Trim().ToLowerInvariant() switch
        {
            "en" => "DELETE",
            "pl" => "USUŃ",
            "ro" => "STERGE",
            "ar" => "حذف",
            _ => "VERWIJDER"
        };

    public static bool Matches(string? language, string? typed)
    {
        if (string.IsNullOrWhiteSpace(typed))
        {
            return false;
        }

        var expected = ExpectedPhrase(language);
        return string.Equals(typed.Trim(), expected, StringComparison.Ordinal);
    }
}
