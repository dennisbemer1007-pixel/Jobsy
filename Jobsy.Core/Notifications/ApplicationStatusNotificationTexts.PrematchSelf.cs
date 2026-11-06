namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy CvViewedSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je hebt de sollicitatie van {candidateName} voor {vacancyTitle} bekeken.",
            $"You viewed {candidateName}'s application for {vacancyTitle}.",
            language);

    public static Copy MutualYesSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je hebt {candidateName} geaccepteerd voor {vacancyTitle}.",
            $"You accepted {candidateName} for {vacancyTitle}.",
            language);
}
