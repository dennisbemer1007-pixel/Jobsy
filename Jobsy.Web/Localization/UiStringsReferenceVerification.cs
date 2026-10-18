namespace Jobsy.Web.Localization;

/// <summary>Work-experience reference verification (candidate + public referent page).</summary>
internal static class UiStringsReferenceVerification
{
    public static void MergeAll(
        IDictionary<string, string> nl,
        IDictionary<string, string> en,
        IDictionary<string, string> pl,
        IDictionary<string, string> ro,
        IDictionary<string, string> ar)
    {
        void Add(string key, string n, string e)
        {
            nl[key] = n;
            en[key] = e;
            pl[key] = e;
            ro[key] = e;
            ar[key] = e;
        }

        Add("Passport.Tab.References",
            "Referenties", "References");
        Add("Passport.Bubble.References",
            "Hier zie je wie je werkervaring al heeft bevestigd.",
            "Here you see who has already confirmed your work experience.");
        Add("RefVerif.Tip.Title",
            "Tip van Lobsy",
            "Tip from Lobsy");
        Add("RefVerif.Tip.Body",
            "Wist je dat je een referent kunt vragen je werkervaring en competenties te bevestigen? Dan krijg je een geverifieerd vinkje.",
            "Did you know you can ask a reference to confirm your work experience and competencies? Then you get a verified checkmark.");
        Add("RefVerif.Tip.Ask",
            "Referent vragen", "Ask a reference");
        Add("RefVerif.Tip.NotNow",
            "Niet nu", "Not now");
        Add("RefVerif.Tip.AskAnother",
            "Nog een referent vragen", "Ask another reference");
        Add("RefVerif.Tab.Intro",
            "Een verzoek blijft 14 dagen geldig. Je referent krijgt hooguit één herinnering.",
            "A request stays valid for 14 days. Your referent gets at most one reminder.");
        Add("RefVerif.Tip.LinkPassport",
            "Naar referenties",
            "Go to references");
        Add("RefVerif.Tab.Empty",
            "Je hebt nog geen referentieverzoeken verstuurd.",
            "You have not sent any reference requests yet.");
        Add("RefVerif.Tab.NewRequest",
            "Nieuwe referent vragen", "Ask a new reference");
        Add("RefVerif.Col.WorkExperience",
            "Werkervaring", "Work experience");
        Add("RefVerif.Tab.PrivacyNote",
            "Alleen bevestigde referenties zijn zichtbaar op je paspoort. Verstuurde en verlopen verzoeken ziet alleen jij.",
            "Only confirmed references are visible on your passport. Sent and expired requests are only visible to you.");
        Add("RefVerif.Subline.Confirmed",
            "Bevestigd op {0}", "Confirmed on {0}");
        Add("RefVerif.Subline.Sent",
            "Verstuurd op {0} · nog {1} dagen geldig", "Sent on {0} · {1} days left");
        Add("RefVerif.Subline.Expired",
            "Verlopen op {0}, geen reactie", "Expired on {0}, no response");
        Add("RefVerif.Action.View",
            "Bekijken", "View");
        Add("RefVerif.Action.Resend",
            "Opnieuw versturen", "Send again");
        Add("RefVerif.Col.Employer",
            "Werkgever", "Employer");
        Add("RefVerif.Col.Role",
            "Functie", "Role");
        Add("RefVerif.Col.Referent",
            "Referent", "Reference person");
        Add("RefVerif.Col.Status",
            "Status", "Status");
        Add("RefVerif.Col.Endorsed",
            "Competenties", "Competencies");
        Add("RefVerif.Status.confirmed",
            "Bevestigd", "Confirmed");
        Add("RefVerif.Status.sent",
            "Verstuurd", "Sent");
        Add("RefVerif.Status.expired",
            "Verlopen", "Expired");
        Add("RefVerif.Status.declined",
            "Afgewezen", "Declined");
        Add("RefVerif.Status.revoked",
            "Ingetrokken", "Withdrawn");
        Add("RefVerif.Revoke",
            "Intrekken", "Withdraw");
        Add("RefVerif.AskReferent",
            "Referent vragen", "Ask a reference");
        Add("RefVerif.VerifiedBadge",
            "Bevestigd door referent", "Confirmed by reference");
        Add("RefVerif.Request.Title",
            "Referent vragen", "Ask a reference");
        Add("RefVerif.Request.Lead",
            "We mailen je referent een persoonlijke link. Contactgegevens blijven privé.",
            "We email your reference a personal link. Contact details stay private.");
        Add("RefVerif.Request.Name",
            "Naam referent", "Reference name");
        Add("RefVerif.Request.Email",
            "E-mail referent", "Reference email");
        Add("RefVerif.Request.Relation",
            "Relatie", "Relationship");
        Add("RefVerif.Relation.leidinggevende",
            "Leidinggevende", "Manager");
        Add("RefVerif.Relation.collega",
            "Collega", "Colleague");
        Add("RefVerif.Relation.opdrachtgever",
            "Opdrachtgever", "Client");
        Add("RefVerif.Relation.anders",
            "Anders", "Other");
        Add("RefVerif.Request.Consent",
            ReferenceVerificationRulesConsentNl(),
            ReferenceVerificationRulesConsentEn());
        Add("RefVerif.Request.Send",
            "Verstuur verzoek", "Send request");
        Add("RefVerif.Request.Sent",
            "De mail is onderweg.", "The email is on its way.");
        Add("RefVerif.Request.Back",
            "Terug naar paspoort", "Back to passport");
        Add("RefVerif.Request.NotFound",
            "Deze werkervaring is niet gevonden.", "This work experience was not found.");
        Add("RefVerif.Request.NotEligible",
            "Je kunt pas referenties vragen als je tests klaar zijn en werkervaring hebt ingevuld.",
            "You can ask for references once your tests are complete and work experience is filled in.");
        Add("RefVerif.Error.Limit",
            "Je hebt het maandlimiet bereikt. Probeer het later opnieuw.",
            "You reached the monthly limit. Try again later.");
        Add("RefVerif.Error.Already",
            "Deze werkervaring is al bevestigd.",
            "This work experience is already confirmed.");
        Add("RefVerif.VerifiedCompetences.Title",
            "Bevestigde competenties", "Confirmed competencies");
        Add("RefVerif.VerifiedCompetences.Disclaimer",
            "Lobsy certificeert zelf niets. Een referent bevestigt wat hij of zij van jouw werk weet.",
            "Lobsy does not certify anything itself. A reference confirms what they know from working with you.");
        Add("RefVerif.VerifiedCompetences.EmployerPreview",
            "Zo ziet een werkgever bevestigde competenties", "How an employer sees confirmed competencies");
        Add("RefVerif.EmployerPreview.CompactTitle",
            "Zo ziet een werkgever het", "How an employer sees it");
        Add("RefVerif.UnverifiedCompetences.Title",
            "Uit je profiel, nog niet bevestigd",
            "From your profile, not yet confirmed");
        Add("RefVerif.VerifiedExperience.Title",
            "Bevestigde werkervaring", "Confirmed work experience");
        Add("RefVerif.Detail.Title",
            "Bevestigde referentie", "Confirmed reference");
        Add("RefVerif.Detail.Intro",
            "{0} bevestigde op {1}.", "{0} confirmed on {1}.");
        Add("RefVerif.Detail.NotFound",
            "Deze referentie is niet gevonden.", "This reference was not found.");
        Add("RefVerif.Detail.Back",
            "Terug naar referenties", "Back to references");
        Add("RefVerif.Detail.FactsTitle",
            "Wat is bevestigd?", "What was confirmed?");
        Add("RefVerif.Detail.FactEmployer",
            "Werk bij {0}", "Worked at {0}");
        Add("RefVerif.Detail.FactPeriod",
            "Periode klopt", "Period is correct");
        Add("RefVerif.Detail.FactRole",
            "Functie {0}", "Role {0}");
        Add("RefVerif.Detail.FactYes",
            "Ja", "Yes");
        Add("RefVerif.Detail.FactNo",
            "Nee", "No");
        Add("RefVerif.Detail.CommentTitle",
            "Opmerking van je referent", "Comment from your reference");
        Add("RefVerif.Detail.CommentPrivate",
            "Alleen jij ziet dit.", "Only you see this.");
        Add("RefVerif.Detail.CommentEmpty",
            "Geen opmerking gegeven.", "No comment provided.");
        Add("RefVerif.Public.PageTitle",
            "Werkreferentie", "Work reference");
        Add("RefVerif.Public.Intro",
            "{0} vraagt je om werkervaring te bevestigen.",
            "{0} asks you to confirm work experience.");
        Add("RefVerif.Public.Greeting",
            "Hallo {0}, {1} vraagt je om een referentie",
            "Hello {0}, {1} is asking you for a reference");
        Add("RefVerif.Public.IntroDetail",
            "{0} noemde jou als {2} bij {1}. Het duurt ongeveer 2 minuten. Je hoeft alleen in te vullen wat jij weet.",
            "{0} named you as their {2} at {1}. It takes about 2 minutes. Only fill in what you know.");
        Add("RefVerif.Public.Privacy",
            "Lobsy vraagt dit omdat deze persoon jou noemde als referent. Je antwoorden gaan naar die persoon. Je hoeft niet te antwoorden.",
            "Lobsy asks because this person named you as a reference. Your answers go to them. You do not have to respond.");
        Add("RefVerif.Public.PrivacyShort",
            "Geen account nodig. Je e-mailadres zien werkgevers niet.",
            "No account needed. Employers do not see your email address.");
        Add("RefVerif.Public.SectionFacts",
            "Klopt dit?", "Is this correct?");
        Add("RefVerif.Public.SectionFactsLead",
            "Alleen feiten over haar werk bij jullie.", "Only facts about their work with you.");
        Add("RefVerif.Public.SectionCompetences",
            "Herken je deze competenties?", "Do you recognise these competencies?");
        Add("RefVerif.Public.SectionCompetencesLead",
            "Deze komen uit de tests die {0} heeft gedaan. Vink alleen aan wat je in de praktijk hebt gezien.",
            "These come from tests {0} took. Only tick what you have seen in practice.");
        Add("RefVerif.Public.CompetenceCounter",
            "{0} / {1} gekozen", "{0} / {1} selected");
        Add("RefVerif.Public.SectionComment",
            "Wil je nog iets toevoegen?", "Anything else to add?");
        Add("RefVerif.Public.Optional",
            "optioneel", "optional");
        Add("RefVerif.Public.CommentHint",
            "Korte toelichting", "Short comment");
        Add("RefVerif.Public.CommentPlaceholder",
            "Bijvoorbeeld een korte toelichting voor {0}…",
            "For example a short note for {0}…");
        Add("RefVerif.Public.CommentPrivate",
            "Alleen {0} ziet je opmerking. Werkgevers zien die niet.",
            "Only {0} sees your comment. Employers do not.");
        Add("RefVerif.Public.Q.EmployerShort",
            "Werkte {0} bij {1}?", "Did {0} work at {1}?");
        Add("RefVerif.Public.Q.PeriodDetail",
            "Van {0}?", "From {0}?");
        Add("RefVerif.Public.Q.RoleDetail",
            "Als {0}?", "As {0}?");
        Add("RefVerif.Public.PeriodPresent",
            "heden", "present");
        Add("RefVerif.Public.Employer",
            "Werkgever", "Employer");
        Add("RefVerif.Public.Role",
            "Functie", "Role");
        Add("RefVerif.Public.Period",
            "Periode", "Period");
        Add("RefVerif.Public.Duties",
            "Werkzaamheden", "Duties");
        Add("RefVerif.Public.Q.Employer",
            "Klopt het dat {0} bij {1} werkte?",
            "Is it true that {0} worked at {1}?");
        Add("RefVerif.Public.Q.Period",
            "Klopt de periode?", "Is the period correct?");
        Add("RefVerif.Public.Q.Role",
            "Klopt de functie?", "Is the role correct?");
        Add("RefVerif.Public.Q.Duties",
            "Kloppen de werkzaamheden ongeveer?", "Are the duties roughly correct?");
        Add("RefVerif.Public.Competences",
            "Welke sterke punten herken je? (max. 5)",
            "Which strengths do you recognise? (max. 5)");
        Add("RefVerif.Public.Comment",
            "Optioneel: korte toelichting", "Optional: short comment");
        Add("RefVerif.Public.Yes",
            "Ja", "Yes");
        Add("RefVerif.Public.No",
            "Nee", "No");
        Add("RefVerif.Public.SkipDuties",
            "Weet ik niet / niet van toepassing", "Not sure / not applicable");
        Add("RefVerif.Public.Submit",
            "Versturen", "Submit");
        Add("RefVerif.Public.Decline",
            "Ik wil niet meedoen", "I do not want to take part");
        Add("RefVerif.Public.Unknown",
            "Ik ken deze persoon niet", "I do not know this person");
        Add("RefVerif.Public.Thanks",
            "Bedankt. Je antwoord is verstuurd.",
            "Thank you. Your response has been sent.");
        Add("RefVerif.Public.ThanksDecline",
            "Je hoefde niet mee te doen — bedankt voor het laten weten.",
            "You did not have to take part — thanks for letting us know.");
        Add("RefVerif.Public.Invalid",
            "Deze link is ongeldig.", "This link is invalid.");
        Add("RefVerif.Public.Expired",
            "Deze link is verlopen.", "This link has expired.");
        Add("RefVerif.Public.Used",
            "Deze link is al gebruikt.", "This link has already been used.");
        Add("RefVerif.Public.AnswersMissing",
            "Beantwoord alle verplichte vragen.", "Answer all required questions.");
        Add("RefVerif.Public.Seo.Title",
            "Werkreferentie · Lobsy", "Work reference · Lobsy");
        Add("RefVerif.Public.Seo.Description",
            "Bevestig werkervaring voor iemand die jou als referent noemde.",
            "Confirm work experience for someone who named you as a reference.");
    }

    private static string ReferenceVerificationRulesConsentNl()
        => Jobsy.Core.Rules.ReferenceVerificationRules.CandidateConsentText;

    private static string ReferenceVerificationRulesConsentEn()
        => "I ask this person to confirm my work experience and competencies. Lobsy sends an email with a personal link. My reference's contact details stay private.";
}
