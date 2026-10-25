namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy CandidateWithdrawn(string candidateName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Application withdrawn",
                $"{candidateName} withdrew the application for {vacancyTitle}.")
            : new Copy(
                "Sollicitatie ingetrokken",
                $"{candidateName} heeft de sollicitatie voor {vacancyTitle} ingetrokken.");

    public static Copy ApplicationWithdrawnSelf(string companyName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je hebt je sollicitatie voor {vacancyTitle} bij {companyName} ingetrokken.",
            $"You withdrew your application for {vacancyTitle} at {companyName}.",
            language);
}
