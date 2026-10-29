namespace Jobsy.Web.Localization;

public static class UiStringsOuder
{
    public static void MergeNlEn(IDictionary<string, string> nl, IDictionary<string, string> en)
    {
        nl["Ouder.Seo.Title"] = "Leerlingpaspoort voor ouders";
        en["Ouder.Seo.Title"] = "Pupil passport for parents";

        nl["Ouder.Page.Title"] = "Leerlingpaspoort bekijken";
        en["Ouder.Page.Title"] = "View the pupil passport";

        nl["Ouder.Page.Lead"] =
            "Vul de leerlingcode in die je van school kreeg. Je ziet daarna het paspoort alleen om te lezen.";
        en["Ouder.Page.Lead"] =
            "Enter the pupil code you received from school. You can then read the passport.";

        nl["Ouder.Form.Code"] = "Leerlingcode";
        en["Ouder.Form.Code"] = "Pupil code";

        nl["Ouder.Form.Submit"] = "Paspoort openen";
        en["Ouder.Form.Submit"] = "Open passport";

        nl["Ouder.Error.Invalid"] =
            "Deze code klopt niet of is niet meer geldig. Vraag de leraar om een nieuwe code.";
        en["Ouder.Error.Invalid"] =
            "This code is not valid or has expired. Ask the teacher for a new code.";

        nl["Ouder.Error.Cooldown"] = "Even wachten en opnieuw proberen.";
        en["Ouder.Error.Cooldown"] = "Please wait a moment and try again.";

        nl["Ouder.Error.Retry"] = "Probeer het opnieuw.";
        en["Ouder.Error.Retry"] = "Please try again.";

        nl["Ouder.Error.Staff"] = "Log eerst uit als medewerker of kandidaat.";
        en["Ouder.Error.Staff"] = "Sign out as staff or candidate first.";

        nl["Ouder.Error.Expired"] = "Je sessie is verlopen. Vul de code opnieuw in.";
        en["Ouder.Error.Expired"] = "Your session has expired. Enter the code again.";

        nl["Ouder.Passport.Pdf"] = "Paspoort (pdf)";
        en["Ouder.Passport.Pdf"] = "Passport (PDF)";

        nl["Ouder.Answers.Pdf"] = "Antwoorden (pdf)";
        en["Ouder.Answers.Pdf"] = "Answers (PDF)";

        nl["Ouder.Downloads.Aria"] = "Downloads";
        en["Ouder.Downloads.Aria"] = "Downloads";

        nl["Ouder.Passport.OtherCode"] = "Andere code";
        en["Ouder.Passport.OtherCode"] = "Different code";

        nl["Ouder.FeatureDisabled"] = "Deze pagina is nu niet beschikbaar.";
        en["Ouder.FeatureDisabled"] = "This page is not available right now.";
    }
}
