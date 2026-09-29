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
        nl["School.DashboardEmpty"] = "Nog geen klassen. Maak je eerste klas aan.";
        nl["School.DashboardLead"] = "Klassen, codes en resultaten verschijnen hier zodra je klassen aanmaakt.";
        nl["School.DashboardIntro"] = "Lobsy bewaart geen namen. Codes koppel je zelf aan leerlingen.";
        nl["School.Cancel"] = "Annuleren";
        nl["School.Save"] = "Opslaan";
        nl["School.Filter.All"] = "Alle";

        nl["School.Kpi.Classes"] = "Klassen";
        nl["School.Kpi.Codes"] = "Leerlingcodes";
        nl["School.Kpi.CodesNote"] = "geen namen bij Lobsy";
        nl["School.Kpi.Completed"] = "Test afgerond";
        nl["School.Kpi.Teachers"] = "Leraren";
        nl["School.Kpi.TeachersNoMfa"] = "{0} zonder 2FA";

        nl["School.Dash.TestsPerClass"] = "Tests per klas";
        nl["School.Dash.Todo"] = "Te doen";
        nl["School.Dash.TodoAll"] = "Alles bekijken";
        nl["School.Dash.Interests"] = "Interesses hele school";
        nl["School.Dash.InterestsEmpty"] = "Zichtbaar vanaf 5 afgeronde tests.";

        nl["School.Todo.Title"] = "Te doen";
        nl["School.Todo.Lead"] = "Acties die aandacht vragen op school.";
        nl["School.Todo.Empty"] = "Alles bij.";

        nl["School.Col.Class"] = "Klas";
        nl["School.Col.Level"] = "Niveau";
        nl["School.Col.Year"] = "Leerjaar";
        nl["School.Col.Teacher"] = "Leraar";
        nl["School.Col.Codes"] = "Codes";
        nl["School.Col.Started"] = "Gestart";
        nl["School.Col.Completed"] = "Afgerond";
        nl["School.Col.Progress"] = "Voortgang";
        nl["School.Col.Window"] = "Testvenster";
        nl["School.Col.Parents"] = "Ouders";
        nl["School.Col.Code"] = "Code";
        nl["School.Col.Status"] = "Status";
        nl["School.Col.LastActive"] = "Laatst actief";

        nl["School.Class.Title"] = "Klassen & codes";
        nl["School.Class.Lead"] = "Maak klassen aan, print de codelijst en beheer het testvenster.";
        nl["School.Class.New"] = "Nieuwe klas";
        nl["School.Class.Empty"] = "Nog geen klassen dit schooljaar.";
        nl["School.Class.NotFound"] = "Klas niet gevonden.";
        nl["School.Class.Field.Name"] = "Klasnaam";
        nl["School.Class.Field.Level"] = "Niveau";
        nl["School.Class.Field.Year"] = "Leerjaar";
        nl["School.Class.Field.Count"] = "Aantal leerlingen";
        nl["School.Class.Field.Teachers"] = "Leraar(en)";
        nl["School.Class.CodesNote"] = "Lobsy maakt voor elke leerling een code. Namen vul je zelf in op de geprinte lijst.";
        nl["School.Class.Danger"] = "Klas verwijderen";
        nl["School.Class.Delete"] = "Klas verwijderen";
        nl["School.Class.DeleteConfirm"] = "Typ de klasnaam ter bevestiging";

        nl["School.CodeList.Pdf"] = "Codelijst printen (PDF)";
        nl["School.CodeList.Csv"] = "Download CSV";
        nl["School.CodeList.Add"] = "Codes erbij";

        nl["School.Codes.Title"] = "Leerlingcodes";
        nl["School.Codes.Empty"] = "Nog geen codes.";
        nl["School.Codes.Replace"] = "Nieuwe code";
        nl["School.Codes.Delete"] = "Code verwijderen";
        nl["School.Codes.ReplaceConfirm"] = "De oude code werkt dan niet meer. De antwoorden blijven bewaard.";
        nl["School.Codes.DeleteConfirm"] = "Alle antwoorden en uitkomsten van deze code worden direct verwijderd. Gebruik dit bij bezwaar van ouders of leerling. Dit kan niet ongedaan worden.";

        nl["School.Status.NotStarted"] = "Nog niet gestart";
        nl["School.Status.InProgress"] = "Bezig {0}/{1}";
        nl["School.Status.Completed"] = "Afgerond";

        nl["School.Parents.Title"] = "Ouders informeren";
        nl["School.Parents.Body"] = "Lobsy is een hulpmiddel van school. De school is verantwoordelijk voor de gegevens; Lobsy verwerkt ze in opdracht. Informeer ouders vóór de test en geef ze de kans bezwaar te maken.";
        nl["School.Parents.LetterLink"] = "Ouderbrief (voorbeeld)";
        nl["School.Parents.ConfirmCheckbox"] = "Ik bevestig: ouders van klas {0} zijn geïnformeerd";
        nl["School.Parents.Confirm"] = "Bevestigen";
        nl["School.Parents.Confirmed"] = "Bevestigd";
        nl["School.Parents.Pending"] = "Nog bevestigen";
        nl["School.Parents.ConfirmedOn"] = "Bevestigd op";
        nl["School.Parents.By"] = "door";
        nl["School.Parents.Withdraw"] = "Bevestiging intrekken";

        nl["School.TestWindow.Title"] = "Testvenster";
        nl["School.TestWindow.Lead"] = "Leerlingen kunnen alleen inloggen terwijl het venster open is.";
        nl["School.TestWindow.Open"] = "Openen";
        nl["School.TestWindow.Close"] = "Sluiten";
        nl["School.TestWindow.Reopen"] = "Opnieuw openen";
        nl["School.TestWindow.ClosesOn"] = "Sluit op (optioneel)";
        nl["School.TestWindow.Until"] = "t/m";
        nl["School.TestWindow.StatusOpen"] = "Open";
        nl["School.TestWindow.StatusClosed"] = "Dicht";
        nl["School.TestWindow.StatusNotOpen"] = "Nog niet open";
        nl["School.TestWindow.FixLink"] = "Naar de oplossing";

        nl["School.Teachers.Title"] = "Leraren";
        nl["School.Teachers.Lead"] = "2FA is verplicht voor iedereen met toegang tot resultaten.";
        nl["School.Teachers.Invite"] = "Leraar uitnodigen";
        nl["School.Teachers.InviteLead"] = "De leraar ontvangt een e-mail en stelt eerst 2FA in.";
        nl["School.Teachers.Empty"] = "Nog geen leraren.";
        nl["School.Teachers.Col.Name"] = "Naam";
        nl["School.Teachers.Col.Email"] = "E-mail";
        nl["School.Teachers.Col.Classes"] = "Eigen klassen";
        nl["School.Teachers.Col.Mfa"] = "2FA";
        nl["School.Teachers.Col.Status"] = "Status";
        nl["School.Teachers.Col.LastLogin"] = "Laatst ingelogd";
        nl["School.Teachers.MfaOn"] = "Aan";
        nl["School.Teachers.MfaOff"] = "Nog instellen";
        nl["School.Teachers.StatusInvited"] = "Uitgenodigd";
        nl["School.Teachers.StatusActive"] = "Actief";
        nl["School.Teachers.Assign"] = "Klassen wijzigen";
        nl["School.Teachers.Resend"] = "Opnieuw uitnodigen";
        nl["School.Teachers.Remove"] = "Verwijderen";
        nl["School.Teachers.Field.Name"] = "Naam";
        nl["School.Teachers.Field.Email"] = "School-e-mail";
        nl["School.Teachers.Field.Classes"] = "Klassen";
        nl["School.Teachers.DomainHint"] = "moet eindigen op @{0}";
        nl["School.Teachers.ScopeNote"] = "Een leraar ziet alleen de klassen die je hier aanvinkt. Uitslagen per code, verhaal en PDF: alleen van die klassen.";
        nl["School.Teachers.MfaLocked"] = "Tweestapsverificatie · Verplicht";
        nl["School.Teachers.MfaLockedHelp"] = "Na de eerste keer inloggen stelt de leraar een authenticator-app in. Inloggen met Microsoft of Google van school telt ook.";
        nl["School.Teachers.SendInvite"] = "Uitnodiging versturen";

        nl["School.Results.Title"] = "Resultaten";
        nl["School.Results.Lead"] = "Per klas en per code. Namen zie je hier nooit. Die staan alleen op de eigen lijst van de leraar.";
        nl["School.Results.PickClass"] = "Kies een klas.";
        nl["School.Results.Totals"] = "Totalen per klas";
        nl["School.Results.TotalsHidden"] = "Zichtbaar vanaf 5 afgeronde tests.";
        nl["School.Results.Riasec"] = "RIASEC top-3";
        nl["School.Results.Values"] = "Top-drijfveren";
        nl["School.Results.DreamJobs"] = "Droombanen";
        nl["School.Results.PerCode"] = "Per code";
        nl["School.Results.Interest"] = "Interessecode";
        nl["School.Results.TopValue"] = "Top-drijfveer";
        nl["School.Results.DreamJob"] = "Droombaan";
        nl["School.Results.WhatSchoolSees"] = "Wat de school ziet";
        nl["School.Results.PerCodeOn"] = "Per code: aan";
        nl["School.Results.PerCodeOff"] = "Per code: uit";
        nl["School.Results.PerCodeOnNote"] = "Lobsy-beheer kan 'resultaten per code' uitzetten. Dan zie je alleen totalen per klas (vanaf 5 leerlingen).";
        nl["School.Results.PerCodeOffNote"] = "Je ziet alleen totalen per klas. Resultaten per code ziet de leraar.";
        nl["School.Results.TeacherOnlyNote"] = "Verhaal, likes en PDF per code ziet alleen de leraar van de klas.";

        nl["School.Details.Title"] = "Schoolgegevens";
        nl["School.Details.Lead"] = "Alleen-lezen. Wijzigingen via Lobsy.";
        nl["School.Details.Name"] = "Naam";
        nl["School.Details.City"] = "Plaats";
        nl["School.Details.Brin"] = "BRIN";
        nl["School.Details.Domains"] = "E-maildomeinen";
        nl["School.Details.ContactLobsy"] = "Wijzigen? Neem contact op met Lobsy.";

        nl["School.Privacy.Title"] = "Privacy & ouders";
        nl["School.Privacy.Lead"] = "Verwerkersovereenkomst, bewaartermijn en ouderbrief.";
        nl["School.Privacy.Agreement"] = "Verwerkersovereenkomst";
        nl["School.Privacy.AgreementMissing"] = "Nog niet geregistreerd";
        nl["School.Privacy.Roles"] = "De school is verwerkingsverantwoordelijke; Lobsy is verwerker.";
        nl["School.Privacy.Retention"] = "Leerlinggegevens worden op {0} na het schooljaar {1} verwijderd. Anonieme totalen (vanaf 5 leerlingen) blijven voor rapportage.";
        nl["School.Privacy.Confirmations"] = "Bevestigingen per klas";
        nl["School.Privacy.Ouderbrief"] = "Ouderbrief";
        nl["School.Privacy.Copy"] = "Tekst kopiëren";

        nl["School.Material.Title"] = "Lesbrief & materiaal";
        nl["School.Material.Lead"] = "Lesbrief en hoe leerlingen inloggen.";
        nl["School.Material.Lesbrief"] = "Lesbrief";
        nl["School.Material.LesbriefBody"] = "Korte handleiding voor in de les: codes uitdelen, testvenster, pauze en afronden.";
        nl["School.Material.OpenLesbrief"] = "Lesbrief openen";
        nl["School.Material.OnDemandNote"] = "De lesbrief wordt on-demand als HTML/PDF getoond (niet vooraf gegenereerd).";
        nl["School.Material.LoginSteps"] = "Zo loggen leerlingen in";
        nl["School.Material.Step1"] = "Ga naar lobsy.nl/leerling";
        nl["School.Material.Step2"] = "Kies school en klas";
        nl["School.Material.Step3"] = "Vul de code van de papieren lijst in";
        nl["School.Material.Step4"] = "Start de ontdekkingsreis (ca. 25 minuten)";

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
