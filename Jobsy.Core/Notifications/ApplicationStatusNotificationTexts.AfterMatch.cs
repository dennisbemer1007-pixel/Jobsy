namespace Jobsy.Core.Notifications;

public static partial class ApplicationStatusNotificationTexts
{
    public static Copy SelfConfirmation(string bodyNl, string bodyEn, string language)
        => IsEnglish(language)
            ? new Copy("Confirmation", bodyEn)
            : new Copy("Bevestiging", bodyNl);

    public static Copy InterviewSlotsProposed(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Times proposed",
                $"{companyName} proposed times for an interview for {vacancyTitle}. Pick a time that works for you.")
            : new Copy(
                "Momenten voorgesteld",
                $"{companyName} stelde momenten voor een gesprek voor {vacancyTitle}. Kies een moment dat past.");

    public static Copy InterviewSlotsProposedSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je stelde momenten voor het gesprek met {candidateName} over {vacancyTitle}.",
            $"You proposed times for the interview with {candidateName} for {vacancyTitle}.",
            language);

    public static Copy InterviewSlotChosenByCandidate(string candidateName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Time chosen",
                $"{candidateName} chose a time for the interview about {vacancyTitle}.")
            : new Copy(
                "Moment gekozen",
                $"{candidateName} koos een moment voor het gesprek over {vacancyTitle}.");

    public static Copy InterviewSlotChosenByCandidateSelf(string companyName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je koos een moment voor het gesprek over {vacancyTitle} bij {companyName}.",
            $"You chose a time for your interview for {vacancyTitle} at {companyName}.",
            language);

    public static Copy InterviewSlotChosenByEmployer(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Time chosen",
                $"{companyName} confirmed the chosen time for {vacancyTitle}.")
            : new Copy(
                "Moment gekozen",
                $"{companyName} bevestigde het gekozen moment voor {vacancyTitle}.");

    public static Copy InterviewSlotChosenByEmployerSelf(string candidateName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je bevestigde het moment voor het gesprek met {candidateName} over {vacancyTitle}.",
            $"You confirmed the time for the interview with {candidateName} for {vacancyTitle}.",
            language);

    public static Copy InterviewAlternativeProposed(string candidateName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Another time proposed",
                $"{candidateName} proposed another time for the interview about {vacancyTitle}. Review the proposal.")
            : new Copy(
                "Ander moment voorgesteld",
                $"{candidateName} stelde een ander moment voor het gesprek over {vacancyTitle}. Bekijk het voorstel.");

    public static Copy InterviewAlternativeProposedSelf(string companyName, string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je stelde een ander moment voor het gesprek over {vacancyTitle} bij {companyName}.",
            $"You proposed another time for your interview for {vacancyTitle} at {companyName}.",
            language);

    public static Copy InterviewScheduledCandidate(string companyName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Interview scheduled",
                $"Your interview for {vacancyTitle} at {companyName} is scheduled.")
            : new Copy(
                "Gesprek gepland",
                $"Je gesprek voor {vacancyTitle} bij {companyName} staat gepland.");

    public static Copy InterviewScheduledEmployer(string candidateName, string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Interview scheduled",
                $"The interview with {candidateName} for {vacancyTitle} is scheduled.")
            : new Copy(
                "Gesprek gepland",
                $"Het gesprek met {candidateName} over {vacancyTitle} staat gepland.");

    public static Copy InterviewRescheduleRequested(string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Interview moved",
                $"The other party wants to move the interview for {vacancyTitle}. See the reason and plan again.")
            : new Copy(
                "Gesprek verzet",
                $"De andere partij wil het gesprek voor {vacancyTitle} verzetten. Bekijk de reden en plan opnieuw.");

    public static Copy InterviewRescheduleRequestedSelf(string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je vroeg om het gesprek voor {vacancyTitle} te verzetten.",
            $"You asked to move the interview for {vacancyTitle}.",
            language);

    public static Copy InterviewCancelled(string vacancyTitle, string language)
        => IsEnglish(language)
            ? new Copy(
                "Interview cancelled",
                $"The interview for {vacancyTitle} was cancelled. See the reason in the thread.")
            : new Copy(
                "Gesprek afgezegd",
                $"Het gesprek voor {vacancyTitle} is afgezegd. Bekijk de reden in het dossier.");

    public static Copy InterviewCancelledSelf(string vacancyTitle, string language)
        => SelfConfirmation(
            $"Je hebt het gesprek voor {vacancyTitle} afgezegd.",
            $"You cancelled the interview for {vacancyTitle}.",
            language);
}
