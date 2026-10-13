using Jobsy.Core.Localization;

namespace Jobsy.Core.Ai;

/// <summary>
/// Shown when AI was asked for but cannot run. Plain B1. No provider name, no key.
/// </summary>
public static class AiUnavailableCopy
{
    public const string Dutch = "Dit werkt nu even niet. Probeer het later nog eens.";

    public static string For(string? language)
        => JobsyLanguages.Normalize(language) switch
        {
            "en" => "This does not work right now. Try again later.",
            "pl" => "To teraz nie działa. Spróbuj później jeszcze raz.",
            "ro" => "Asta nu merge acum. Încearcă din nou mai târziu.",
            "ar" => "هذا لا يعمل الآن. حاول مرة أخرى لاحقاً.",
            _ => Dutch
        };
}
