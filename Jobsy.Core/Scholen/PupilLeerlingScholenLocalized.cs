namespace Jobsy.Core.Scholen;

public static class PupilLeerlingScholenLocalized
{
    public static string Get(string key, string? language)
    {
        if (key.StartsWith("LeerlingQ.", StringComparison.Ordinal))
        {
            return PupilQuestionBankNlStrings.Get(key);
        }

        if (PupilLeerlingScholenStrings.TryGet(key, language, out var text))
        {
            return text;
        }

        return PupilPassport.PupilPassportLocalizedCopy.Get(key, language ?? "nl");
    }
}
