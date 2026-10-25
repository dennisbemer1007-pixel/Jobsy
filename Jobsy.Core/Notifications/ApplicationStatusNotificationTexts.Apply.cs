namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy EmployerNewApplication(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "New application",
                $"There is a new application for {vacancyTitle} at {companyName}.")
            : new Copy(
                "Nieuwe sollicitatie",
                $"Er is een nieuwe sollicitatie voor {vacancyTitle} bij {companyName}.");

    public static Copy ApplicationSubmittedSelf(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Application sent",
                $"Your application for {vacancyTitle} at {companyName} has been sent.")
            : new Copy(
                "Sollicitatie verstuurd",
                $"Je sollicitatie voor {vacancyTitle} bij {companyName} is verstuurd.");
}
