namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy ApplicationOutcomeOffer(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Job offer",
                $"{companyName} made an offer for {vacancyTitle}. Read the details in your thread.")
            : new Copy(
                "Aanbod",
                $"{companyName} heeft een aanbod gedaan voor {vacancyTitle}. Lees de toelichting in je dossier.");

    public static Copy ApplicationOutcomeOfferSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je deelde een aanbod met {candidateName} voor {vacancyTitle}.",
            $"You shared an offer with {candidateName} for {vacancyTitle}.",
            language);

    public static Copy ApplicationOutcomeNextRound(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Another interview",
                $"{companyName} wants another interview for {vacancyTitle}. See what happens next.")
            : new Copy(
                "Nog een gesprek",
                $"{companyName} wil nog een gesprek voor {vacancyTitle}. Bekijk wat er verder gebeurt.");

    public static Copy ApplicationOutcomeNextRoundSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je gaf aan dat er nog een gesprek volgt met {candidateName} voor {vacancyTitle}.",
            $"You indicated another interview follows with {candidateName} for {vacancyTitle}.",
            language);

    public static Copy ApplicationOutcomeNotNow(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Not now",
                $"{companyName} shared the outcome for {vacancyTitle}. Read the explanation in your thread.")
            : new Copy(
                "Nu niet",
                $"{companyName} heeft de uitkomst voor {vacancyTitle} gedeeld. Lees de toelichting in je dossier.");

    public static Copy ApplicationOutcomeNotNowSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je deelde de uitkomst met {candidateName} voor {vacancyTitle}.",
            $"You shared the outcome with {candidateName} for {vacancyTitle}.",
            language);
}
