namespace Jobsy.Web.Localization;

internal static class UiStringsAi
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        nl["Ai.GeneratedLabel"] =
            "Gemaakt met AI. Klopt iets niet? Pas het aan of meld het.";
        en["Ai.GeneratedLabel"] =
            "Made with AI. Something wrong? Edit it or let us know.";
        pl["Ai.GeneratedLabel"] =
            "Utworzone z AI. Coś jest nie tak? Popraw lub daj znać.";
        ro["Ai.GeneratedLabel"] =
            "Creat cu AI. Ceva nu e în regulă? Editează sau spune-ne.";
        ar["Ai.GeneratedLabel"] =
            "صُنع بالذكاء الاصطناعي. هل يوجد خطأ؟ عدّل النص أو أخبرنا.";
    }
}