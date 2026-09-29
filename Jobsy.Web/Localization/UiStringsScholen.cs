namespace Jobsy.Web.Localization;

/// <summary>
/// Dutch-only Scholen UI strings (D12). Other languages fall back to nl via <see cref="UiStrings.Get"/>.
/// Prefixes School. / Leraar. / Leerling. / AdminScholen. are exempt from localization parity.
/// </summary>
public static class UiStringsScholen
{
    public static void MergeNl(Dictionary<string, string> nl)
    {
        // —— School shell ——
        nl["School.ProductLabel"] = "Lobsy voor scholen";
        nl["School.RoleChip"] = "Schoolbeheerder";
        nl["School.DashboardTitle"] = "Dashboard";
        nl["School.DashboardEmpty"] = "Hier komt je dashboard";
        nl["School.DashboardLead"] = "Klassen, codes en resultaten verschijnen hier zodra je klassen aanmaakt.";
        nl["School.Nav.Overview"] = "Overzicht";
        nl["School.Nav.Dashboard"] = "Dashboard";
        nl["School.Nav.Todo"] = "Te doen";
        nl["School.Nav.Pupils"] = "Leerlingen";
        nl["School.Nav.Classes"] = "Klassen & codes";
        nl["School.Nav.Results"] = "Resultaten";
        nl["School.Nav.Team"] = "Team";
        nl["School.Nav.Teachers"] = "Leraren";
        nl["School.Nav.School"] = "School";
        nl["School.Nav.Details"] = "Schoolgegevens";
        nl["School.Nav.Privacy"] = "Privacy & ouders";
        nl["School.Nav.Materials"] = "Lesbrief & materiaal";
        nl["School.Nav.More"] = "Meer";
        nl["School.Nav.Help"] = "Hulp voor scholen";
        nl["School.Footer.NoNames"] = "Lobsy kent geen leerlingnamen. Alleen codes. De namenlijst houdt de school zelf.";
        nl["School.FeatureDisabled"] = "Scholen is nog niet ingeschakeld.";

        // —— Leraar shell ——
        nl["Leraar.RoleChip"] = "Leraar";
        nl["Leraar.DashboardTitle"] = "Mijn klas";
        nl["Leraar.DashboardEmpty"] = "Hier komt je dashboard";
        nl["Leraar.NoClass"] = "Je hebt nog geen klas. Vraag je schoolbeheerder om je aan een klas te koppelen.";
        nl["Leraar.Nav.MyClass"] = "Mijn klas";
        nl["Leraar.Nav.Overview"] = "Klasoverzicht";
        nl["Leraar.Nav.Codes"] = "Leerlingcodes";
        nl["Leraar.Nav.Group"] = "Groepsresultaten";
        nl["Leraar.Nav.DreamJobs"] = "Droombanen";
        nl["Leraar.Nav.InClass"] = "In de les";
        nl["Leraar.Nav.TestWindow"] = "Testvenster";
        nl["Leraar.Nav.Materials"] = "Codelijst & lesbrief";
        nl["Leraar.Nav.MyClasses"] = "Mijn klassen";
        nl["Leraar.Nav.More"] = "Meer";

        // —— Leerling (shell placeholders for later files) ——
        nl["Leerling.LoginTitle"] = "Inloggen met code";

        // —— Admin Scholen ——
        nl["AdminScholen.Nav"] = "Scholen";
        nl["AdminScholen.ListTitle"] = "Scholen";
        nl["AdminScholen.ListLead"] = "Maak scholen aan, registreer de verwerkersovereenkomst en nodig schoolbeheerders uit.";
        nl["AdminScholen.Add"] = "School toevoegen";
        nl["AdminScholen.Col.Name"] = "Naam";
        nl["AdminScholen.Col.City"] = "Plaats";
        nl["AdminScholen.Col.Brin"] = "BRIN";
        nl["AdminScholen.Col.Status"] = "Status";
        nl["AdminScholen.Col.Agreement"] = "Verwerkersovereenkomst";
        nl["AdminScholen.Col.Classes"] = "Klassen";
        nl["AdminScholen.Col.Teachers"] = "Leraren";
        nl["AdminScholen.Status.Active"] = "Actief";
        nl["AdminScholen.Status.Inactive"] = "Inactief";
        nl["AdminScholen.Agreement.Missing"] = "Ontbreekt";
        nl["AdminScholen.Empty"] = "Nog geen scholen.";
        nl["AdminScholen.Drawer.Title"] = "School toevoegen";
        nl["AdminScholen.Field.Name"] = "Naam";
        nl["AdminScholen.Field.City"] = "Plaats";
        nl["AdminScholen.Field.Brin"] = "BRIN (optioneel)";
        nl["AdminScholen.Field.Domains"] = "E-maildomeinen";
        nl["AdminScholen.Field.DomainsHelp"] = "Bijv. voorbeeldcollege.nl — komma of Enter om toe te voegen.";
        nl["AdminScholen.Field.Active"] = "Actief";
        nl["AdminScholen.Save"] = "Opslaan";
        nl["AdminScholen.Cancel"] = "Annuleren";
        nl["AdminScholen.Detail.Title"] = "School";
        nl["AdminScholen.Tab.Details"] = "Gegevens";
        nl["AdminScholen.Tab.Agreement"] = "Verwerkersovereenkomst";
        nl["AdminScholen.Tab.Admins"] = "Schoolbeheerders";
        nl["AdminScholen.Tab.Overview"] = "Overzicht";
        nl["AdminScholen.Agreement.Note"] = "De school is verwerkingsverantwoordelijke, Lobsy is verwerker.";
        nl["AdminScholen.Agreement.Date"] = "Ondertekend op";
        nl["AdminScholen.Agreement.Version"] = "Versie";
        nl["AdminScholen.Agreement.Save"] = "Registreren";
        nl["AdminScholen.Invite.Title"] = "Schoolbeheerder uitnodigen";
        nl["AdminScholen.Invite.Name"] = "Naam";
        nl["AdminScholen.Invite.Email"] = "E-mail";
        nl["AdminScholen.Invite.Send"] = "Uitnodigen";
        nl["AdminScholen.Invite.MfaOn"] = "2FA actief";
        nl["AdminScholen.Invite.MfaOff"] = "2FA ontbreekt";
        nl["AdminScholen.Deactivate"] = "School deactiveren";
        nl["AdminScholen.Deactivate.Confirm"] = "Weet je zeker dat je deze school wilt deactiveren? Staff en leerlingen kunnen niet meer inloggen. Data blijft tot de bewaartermijn.";
        nl["AdminScholen.Overview.Classes"] = "Klassen";
        nl["AdminScholen.Overview.Codes"] = "Codes";
        nl["AdminScholen.Overview.Completed"] = "Afgerond";
        nl["AdminScholen.Settings.Group"] = "Scholen";
        nl["AdminScholen.Settings.Enabled"] = "Scholen inschakelen";
        nl["AdminScholen.Settings.EnabledHelp"] = "Zet scholen, leraren en leerlingen aan of uit. Bewaartermijn blijft altijd draaien.";
        nl["AdminScholen.Settings.PerCode"] = "Resultaten per code voor schoolbeheerder";
        nl["AdminScholen.Settings.PerCodeHelp"] = "Schoolbeheerders zien resultaten per code. Uit: alleen totalen per klas (vanaf 5 leerlingen). Leraren zien altijd hun eigen klas.";
        nl["AdminScholen.Settings.Retention"] = "Bewaartermijn-afkapdatum";
        nl["AdminScholen.Settings.RetentionHelp"] = "Op deze dag worden alle leerlinggegevens van het afgelopen schooljaar verwijderd. Anonieme totalen blijven.";
        nl["AdminScholen.Settings.Month"] = "Maand";
        nl["AdminScholen.Settings.Day"] = "Dag";
    }

    /// <summary>True when the key belongs to the Dutch-only Scholen modules (D12).</summary>
    public static bool IsNlOnlyPrefix(string key) =>
        key.StartsWith("School.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("Leraar.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("Leerling.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("LeerlingQ.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("LeerlingStory.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("LeerlingDroom.", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("AdminScholen.", StringComparison.OrdinalIgnoreCase);
}
