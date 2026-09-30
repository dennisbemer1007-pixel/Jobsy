namespace Jobsy.Web.Localization;

/// <summary>UI strings for werkgever-aanmelding (verification, wizard, banners).</summary>
internal static class UiStringsWerkgeverAanmelding
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

        Add("WaBanner.PreviewLine",
            "Voorbeeld · nog niet zichtbaar voor kandidaten",
            "Preview · not yet visible to candidates",
            "Podgląd · jeszcze niewidoczny dla kandydatów",
            "Previzualizare · încă nevăzut de candidați",
            "معاينة · غير مرئي للمرشحين بعد");
    }
}
