using Jobsy.Core.Localization;

namespace Jobsy.Core.Notifications;

/// <summary>
/// In-app and push copy for sollicitatie statusmeldingen (see docs/meldingen/STATUSMELDINGEN.md).
/// </summary>
public static class ApplicationStatusNotificationTexts
{
    public sealed record Copy(string Title, string Body);

    public static Copy CvViewed(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "CV viewed",
                $"{companyName} has viewed your application for {vacancyTitle}.")
            : new Copy(
                "CV bekeken",
                $"{companyName} heeft je sollicitatie voor {vacancyTitle} bekeken.");

    public static Copy MutualYes(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Mutual yes",
                $"{companyName} wants to proceed with your application for {vacancyTitle}. You can plan an interview next.")
            : new Copy(
                "Wederzijds ja",
                $"{companyName} wil verder met je sollicitatie voor {vacancyTitle}. Jullie kunnen nu een gesprek plannen.");

    private static bool IsEnglish(string language)
        => string.Equals(JobsyLanguages.Normalize(language), "en", StringComparison.OrdinalIgnoreCase);
}
