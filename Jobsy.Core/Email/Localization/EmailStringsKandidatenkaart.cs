namespace Jobsy.Core.Email.Localization;

public static class EmailStringsKandidatenkaart
{
    public static IReadOnlyDictionary<string, string> For(string language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? En : Nl;

    private static readonly IReadOnlyDictionary<string, string> Nl = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Email.KkInviteBundled.Subject"] = "Uitnodiging om te solliciteren: {0}",
        ["Email.KkInviteBundled.Preheader"] = "Match {0}% — bekijk de vacature",
        ["Email.KkInviteBundled.Heading"] = "Werkgever nodigt je uit",
        ["Email.KkInviteBundled.Eyebrow"] = "Kandidatenkaart",
        ["Email.KkInviteBundled.P1"] = "{0} nodigt je uit om te solliciteren op {1} (match {2}%).",
        ["Email.KkInviteBundled.P2"] = "Je ontvangt maximaal enkele uitnodigingen per week, gebundeld in één bericht.",
        ["Email.KkInviteBundled.Cta"] = "Bekijk vacature",

        ["Email.KkInviteReminder.Subject"] = "Herinnering: uitnodiging voor {0}",
        ["Email.KkInviteReminder.Preheader"] = "Reageer op je uitnodiging",
        ["Email.KkInviteReminder.Heading"] = "Nog even reageren?",
        ["Email.KkInviteReminder.Eyebrow"] = "Herinnering",
        ["Email.KkInviteReminder.P1"] = "Je kreeg een uitnodiging voor {0} bij {1}. Reageer als je interesse hebt.",
        ["Email.KkInviteReminder.Cta"] = "Naar vacature",

        ["Email.KkInactivity.Subject"] = "Ben je nog op zoek?",
        ["Email.KkInactivity.Preheader"] = "Bevestig of je nog beschikbaar bent",
        ["Email.KkInactivity.Heading"] = "Ben je nog op zoek?",
        ["Email.KkInactivity.Eyebrow"] = "Beschikbaarheid",
        ["Email.KkInactivity.P1"] = "We zagen je een tijd niet. Sta je nog open voor uitnodigingen van werkgevers?",
        ["Email.KkInactivity.Cta"] = "Instellingen",

        ["Email.KkMissedOut.Subject"] = "Vacature vervuld: {0}",
        ["Email.KkMissedOut.Preheader"] = "Drie vergelijkbare vacatures",
        ["Email.KkMissedOut.Heading"] = "Helaas, vacature vervuld",
        ["Email.KkMissedOut.Eyebrow"] = "Gemist",
        ["Email.KkMissedOut.P1"] = "{0} bij {1} is inmiddels vervuld. Hier zijn drie vergelijkbare vacatures:",
        ["Email.KkMissedOut.Item"] = "• {0} ({1})",
        ["Email.KkMissedOut.Cta"] = "Naar banenkaart",

        ["Email.KkWithdrawn.Subject"] = "Uitnodiging ingetrokken: {0}",
        ["Email.KkWithdrawn.Preheader"] = "Vacature is vol",
        ["Email.KkWithdrawn.Heading"] = "Uitnodiging ingetrokken",
        ["Email.KkWithdrawn.Eyebrow"] = "Vacature vol",
        ["Email.KkWithdrawn.P1"] = "De vacature {0} bij {1} is vervuld. Je uitnodiging is daarom ingetrokken.",
        ["Email.KkWithdrawn.Cta"] = "Naar banenkaart",
    };

    private static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Email.KkInviteBundled.Subject"] = "Invitation to apply: {0}",
        ["Email.KkInviteBundled.Preheader"] = "Match {0}% — view the vacancy",
        ["Email.KkInviteBundled.Heading"] = "An employer invited you",
        ["Email.KkInviteBundled.Eyebrow"] = "Candidate map",
        ["Email.KkInviteBundled.P1"] = "{0} invited you to apply for {1} ({2}% match).",
        ["Email.KkInviteBundled.P2"] = "You receive at most a few invitations per week, bundled in one message.",
        ["Email.KkInviteBundled.Cta"] = "View vacancy",

        ["Email.KkInviteReminder.Subject"] = "Reminder: invitation for {0}",
        ["Email.KkInviteReminder.Preheader"] = "Respond to your invitation",
        ["Email.KkInviteReminder.Heading"] = "Still interested?",
        ["Email.KkInviteReminder.Eyebrow"] = "Reminder",
        ["Email.KkInviteReminder.P1"] = "You received an invitation for {0} at {1}. Respond if you are interested.",
        ["Email.KkInviteReminder.Cta"] = "View vacancy",

        ["Email.KkInactivity.Subject"] = "Still looking for work?",
        ["Email.KkInactivity.Preheader"] = "Confirm you are still available",
        ["Email.KkInactivity.Heading"] = "Still looking?",
        ["Email.KkInactivity.Eyebrow"] = "Availability",
        ["Email.KkInactivity.P1"] = "We have not seen you for a while. Are you still open to employer invitations?",
        ["Email.KkInactivity.Cta"] = "Settings",

        ["Email.KkMissedOut.Subject"] = "Vacancy filled: {0}",
        ["Email.KkMissedOut.Preheader"] = "Three similar vacancies",
        ["Email.KkMissedOut.Heading"] = "Vacancy filled",
        ["Email.KkMissedOut.Eyebrow"] = "Missed out",
        ["Email.KkMissedOut.P1"] = "{0} at {1} has been filled. Here are three similar vacancies:",
        ["Email.KkMissedOut.Item"] = "- {0} ({1})",
        ["Email.KkMissedOut.Cta"] = "Open job map",

        ["Email.KkWithdrawn.Subject"] = "Invitation withdrawn: {0}",
        ["Email.KkWithdrawn.Preheader"] = "Vacancy is full",
        ["Email.KkWithdrawn.Heading"] = "Invitation withdrawn",
        ["Email.KkWithdrawn.Eyebrow"] = "Vacancy full",
        ["Email.KkWithdrawn.P1"] = "The vacancy {0} at {1} is full. Your invitation was withdrawn.",
        ["Email.KkWithdrawn.Cta"] = "Open job map",
    };
}
