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
        var normalized = string.Concat(typed.Where(c => !char.IsWhiteSpace(c)));
        return string.Equals(normalized, expected, StringComparison.Ordinal);
    }
}
