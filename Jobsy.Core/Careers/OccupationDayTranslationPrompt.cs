using Jobsy.Core.Localization;

namespace Jobsy.Core.Careers;

/// <summary>
/// One-shot translation of a stored Dutch workday. The candidate path never uses this prompt.
/// </summary>
public static class OccupationDayTranslationPrompt
{
    public static string System(string targetLanguage)
    {
        var target = JobsyLanguages.Get(targetLanguage);
        return
            "You translate a typical Dutch workday into " + target.NativeName + " (" + target.Code + "). "
            + "Plain language, about B1. Short sentences. Warm and clear, not childish. "
            + "Translate only what is in the Dutch JSON. Do not add a fact. "
            + "Do not add an employer, a company name, a city, a town, a wage, a salary, a diploma, a degree, or a person's name. "
            + "Keep the same parts and the same number of highlights. "
            + "Return only JSON: {\"title\":\"...\",\"morning\":\"...\",\"midday\":\"...\",\"afternoon\":\"...\",\"closing\":\"...\",\"highlights\":[\"...\"],\"varies\":\"...\"}";
    }

    public const string Retry =
        "Translate again. Only JSON. Do not add an employer, city, wage, or diploma. Keep every fact from the Dutch text and do not add a new one.";
}
