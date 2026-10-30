namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy ApplicationNewMessage(string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "New message",
                $"There is a new message about your application for {vacancyTitle}.")
            : new Copy(
                "Nieuw bericht",
                $"Er is een nieuw bericht over je sollicitatie voor {vacancyTitle}.");

    public static Copy ApplicationNewMessageSelf(string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je bericht over {vacancyTitle} is verstuurd.",
            $"Your message about {vacancyTitle} was sent.",
            language);
}
