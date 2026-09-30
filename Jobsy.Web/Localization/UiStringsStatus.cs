namespace Jobsy.Web.Localization;

/// <summary>Status / HTTP error page copy (public-pages 01; extended by docs/errors).</summary>
public static class UiStringsStatus
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("Status.NotFound.Title",
            "Deze pagina bestaat niet",
            "This page does not exist",
            "Ta strona nie istnieje",
            "Această pagină nu există",
            "هذه الصفحة غير موجودة");
        Add("Status.NotFound.Lead",
            "Misschien is de link oud of zit er een typfout in.",
            "The link may be old or there may be a typo.",
            "Link może być stary albo zawierać literówkę.",
            "Linkul poate fi vechi sau poate conține o greșeală.",
            "ربما يكون الرابط قديماً أو فيه خطأ إملائي.");
        Add("Status.NotFound.ToMap",
            "Naar de banenkaart",
            "To the job map",
            "Do mapy ofert",
            "Către harta joburilor",
            "إلى خريطة الوظائف");
        Add("Status.NotFound.HowLobsy",
            "Hoe werkt Lobsy?",
            "How does Lobsy work?",
            "Jak działa Lobsy?",
            "Cum funcționează Lobsy?",
            "كيف يعمل Lobsy؟");
        Add("Status.Generic.Title",
            "Er ging iets mis",
            "Something went wrong",
            "Coś poszło nie tak",
            "Ceva nu a mers bine",
            "حدث خطأ ما");
        Add("Status.Generic.Lead",
            "Probeer het zo nog eens, of ga terug naar de banenkaart.",
            "Please try again in a moment, or go back to the job map.",
            "Spróbuj ponownie za chwilę albo wróć do mapy ofert.",
            "Încearcă din nou peste puțin, sau revino la harta joburilor.",
            "حاول مرة أخرى بعد قليل، أو ارجع إلى خريطة الوظائف.");
        Add("Status.Generic.Home",
            "Naar de banenkaart",
            "To the job map",
            "Do mapy ofert",
            "Către harta joburilor",
            "إلى خريطة الوظائف");
    }
}
