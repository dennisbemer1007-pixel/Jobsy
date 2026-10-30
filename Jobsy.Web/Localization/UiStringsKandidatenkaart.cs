namespace Jobsy.Web.Localization;

public static class UiStringsKandidatenkaart
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlVal, string enVal)
        {
            nl[key] = nlVal;
            en[key] = enVal;
            pl[key] = enVal;
            ro[key] = enVal;
            ar[key] = enVal;
        }

        Add("WgNav.Kandidatenkaart", "Kandidatenkaart", "Candidate map");
        Add("Kk.Title", "Kandidatenkaart", "Candidate map");
        Add("Kk.Lead", "Omgekeerde banenkaart: waar vindbare kandidaten passen bij jouw vacature. Alleen geaggregeerde gebieden, nooit losse personen.", "Reverse job map: where discoverable candidates match your vacancy. Aggregated areas only — never individual profiles.");
        Add("Kk.Badge.Active", "Marktinzicht actief", "Market insight active");
        Add("Kk.Badge.Preview", "Voorbeeld", "Preview");
        Add("Kk.Privacy", "Gebieden vanaf 30 opt-in kandidaten. Geen namen of exacte adressen, wel tellingen en matchverdeling.", "Areas from 30+ opt-in candidates. No names or exact addresses — counts and match bands only.");
        Add("Kk.Filter.Vacancy", "Vacature of profiel", "Vacancy or profile");
        Add("Kk.Filter.MinMatch", "Min. matchscore", "Min. match score");
        Add("Kk.Filter.Travel", "Reisafstand", "Travel distance");
        Add("Kk.Filter.More", "Meer filters", "More filters");
        Add("Kk.Filter.Start", "Startdatum", "Start date");
        Add("Kk.Filter.Hours", "Uren per week", "Hours per week");
        Add("Kk.Filter.MobileChip", "Geaggregeerd · min. 30 · alleen actieve kandidaten", "Aggregated · min. 30 · active candidates only");
        Add("Kk.Filter.Apply", "Toepassen", "Apply");
        Add("Kk.ActiveSell.Title", "Alleen actieve kandidaten", "Active candidates only");
        Add("Kk.ActiveSell.Body", "Wie een uitnodiging niet binnen 2 dagen beantwoordt, krijgt een herinnering en verdwijnt daarna van de kaart tot die zich weer beschikbaar meldt. Wie een maand niet actief is, bevestigt eerst dat die nog zoekt. Zo zie je alleen mensen die echt zoeken.", "Candidates who don't respond to an invite within 2 days get a reminder and then leave the map until they opt back in. After a month without activity they confirm they're still looking. You only see people actively searching.");
        Add("Kk.Area.Meta", "{0} kandidaten · {1} boven 80%", "{0} candidates · {1} above 80%");
        Add("Kk.Area.ActiveWeek", "Actief deze week", "Active this week");
        Add("Kk.BelowThreshold", "Met deze filters zijn er geen gebieden met minimaal 30 kandidaten.", "No areas have at least 30 candidates with these filters.");
        Add("Kk.Map.Legend", "≥ 30 kandidaten", "≥ 30 candidates");
        Add("Kk.Mobile.AreasPeek", "Gebieden in de buurt van je vacature", "Areas near your vacancy");
        Add("Kk.Invite.Open", "Uitnodigen om te solliciteren", "Invite to apply");
        Add("Kk.Invite.PreviewTitle", "Uitnodiging voor gebied", "Invite for area");
        Add("Kk.Invite.ModalLead", "Uitnodigingen voor {0} - alleen kandidaten die aan de drempels voldoen.", "Invites for {0} - only candidates who meet the thresholds.");
        Add("Kk.Invite.StatInvited", "Uitnodigingen ({0}%+ match)", "Invitations ({0}%+ match)");
        Add("Kk.Invite.StatWeeklyCap", "Overgeslagen (weeklimiet)", "Skipped (weekly cap)");
        Add("Kk.Invite.StatBelowInvite", "In gebied onder {0}% (geen uitnodiging)", "In area below {0}% (no invite)");
        Add("Kk.Invite.StatPrefOff", "Uitgeschakeld door kandidaat", "Opted out by candidate");
        Add("Kk.Invite.StatRounds", "Verzendrondes", "Send rounds");
        Add("Kk.Invite.FilterNote", "Kaartfilter: {0}%+ · uitnodigingen vanaf {1}%+.", "Map filter: {0}%+ · invites from {1}%+.");
        Add("Kk.Invite.Eligible", "{0} kandidaten met {1}%+ match", "{0} candidates at {1}%+ match");
        Add("Kk.Invite.Staging", "Getrapte verzending: {0} rondes", "Staged send: {0} rounds");
        Add("Kk.Invite.SkippedWeeklyCap", "{0} kandidaten overgeslagen vanwege weeklimiet (2–3 uitnodigingen).", "{0} candidates skipped due to weekly invite cap (2–3 invitations).");
        Add("Kk.Invite.SkippedBelowMatch", "{0} kandidaten in gebied onder 70% match (uitnodiging).", "{0} candidates in area below 70% match (invite threshold).");
        Add("Kk.Invite.Confirm", "Verstuur uitnodigingen", "Send invitations");
        Add("Kk.Invite.Cancel", "Annuleren", "Cancel");
        Add("Kk.Invite.Success", "Uitnodigingen gepland.", "Invitations scheduled.");
        Add("Kk.Preview.Locked", "Prijs en pakket op aanvraag. Neem contact op om volledige marktinzichten te activeren.", "Pricing on request. Contact us to activate full market insight.");
        Add("Kk.Settings.Title", "Uitnodigingen via kandidatenkaart", "Invitations via candidate map");
        Add("Kk.Settings.Lead", "Bepaal hoeveel anonieme uitnodigingen je ontvangt wanneer een werkgever op jouw gebied solliciteert.", "Choose how many anonymous invites you receive when an employer targets your area.");
        Add("Kk.Settings.Off", "Uit", "Off");
        Add("Kk.Settings.Normal", "Normaal (max. 2 per week)", "Normal (max. 2 per week)");
        Add("Kk.Settings.Max", "Maximaal (max. 3 per week)", "Maximum (max. 3 per week)");
        Add("Kk.ReAvail.Title", "Weer vindbaar op de kaart?", "Visible on the map again?");
        Add("Kk.ReAvail.Body", "Je stond op niet beschikbaar. Geen probleem - zodra je weer vindbaar bent, kun je opnieuw meedoen op de kandidatenkaart, als jij dat wilt.", "You were marked unavailable. No worries - when you're discoverable again, you can rejoin the candidate map if you want.");
        Add("Kk.ReAvail.Yes", "Ja, ik ben weer vindbaar", "Yes, I'm discoverable again");
        Add("Kk.ReAvail.Later", "Later", "Later");
        Add("AdminSettings.Kandidatenkaart.Enabled.Title", "Kandidatenkaart (werkgever)", "Candidate map (employer)");
        Add("AdminSettings.Kandidatenkaart.Enabled.Desc", "Omgekeerde banenkaart: geaggregeerde kandidatengebieden en uitnodigingen. Standaard uit.", "Reverse job map: aggregated candidate areas and invites. Off by default.");
        Add("AdminSettings.Kandidatenkaart.Enabled.ImpactOff", "Werkgevers zien geen kandidatenkaart; routes en achtergrondjobs zijn uit.", "Employers won't see the candidate map; routes and background jobs are off.");
        Add("Insights.Tab.Kandidatenkaart", "Kandidatenkaart", "Candidate map");
        Add("CandidateSettings.Kk.Title", "Kandidatenkaart-uitnodigingen", "Candidate map invitations");
        Add("CandidateSettings.Kk.Desc", "Anonieme uitnodigingen van werkgevers in jouw regio.", "Anonymous invites from employers in your area.");
    }
}
