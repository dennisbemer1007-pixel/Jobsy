namespace Jobsy.Web.Localization;

internal static class UiStringsReadAloud
{
    public static void MergeAll(
        IDictionary<string, string> nl,
        IDictionary<string, string> en,
        IDictionary<string, string> pl,
        IDictionary<string, string> ro,
        IDictionary<string, string> ar)
    {
        void Add(string key, string n, string e, string p, string r, string a)
        {
            nl[key] = n;
            en[key] = e;
            pl[key] = p;
            ro[key] = r;
            ar[key] = a;
        }

        Add(
            "ReadAloud.Play",
            "Lees voor",
            "Read aloud",
            "Czytaj na głos",
            "Citește cu voce tare",
            "اقرأ بصوت عالٍ");
        Add(
            "ReadAloud.Stop",
            "Stoppen",
            "Stop reading",
            "Zatrzymaj",
            "Oprește",
            "أوقف القراءة");
        Add(
            "ReadAloud.PlayAria",
            "Lees deze tekst voor",
            "Read this text aloud",
            "Przeczytaj ten tekst na głos",
            "Citește acest text cu voce tare",
            "اقرأ هذا النص بصوت عالٍ");
        Add(
            "ReadAloud.StopAria",
            "Stop met voorlezen",
            "Stop reading aloud",
            "Zatrzymaj czytanie na głos",
            "Oprește citirea cu voce tare",
            "أوقف القراءة بصوت عالٍ");
        Add(
            "ReadAloud.SettingTitle",
            "Voorlezen",
            "Read aloud",
            "Czytanie na głos",
            "Citire cu voce tare",
            "القراءة بصوت عالٍ");
        Add(
            "ReadAloud.SettingHint",
            "Lobsy leest vragen en teksten voor. Dat blijft op jouw apparaat.",
            "Lobsy reads questions and texts aloud. That stays on your device.",
            "Lobsy czyta pytania i teksty na głos. To zostaje na Twoim urządzeniu.",
            "Lobsy citește întrebările și textele cu voce tare. Rămâne pe dispozitivul tău.",
            "يقرأ لوبسي الأسئلة والنصوص بصوت عالٍ. يبقى هذا على جهازك.");
        Add(
            "ReadAloud.On",
            "Voorlezen aan",
            "Read aloud on",
            "Czytanie włączone",
            "Citire pornită",
            "القراءة مفعّلة");
        Add(
            "ReadAloud.Off",
            "Voorlezen uit",
            "Read aloud off",
            "Czytanie wyłączone",
            "Citire oprită",
            "القراءة متوقفة");
        Add(
            "ReadAloud.Unsupported",
            "Voorlezen werkt niet in deze taal op dit apparaat.",
            "Read aloud does not work for this language on this device.",
            "Czytanie na głos nie działa w tym języku na tym urządzeniu.",
            "Citirea cu voce tare nu merge în această limbă pe acest dispozitiv.",
            "القراءة بصوت عالٍ لا تعمل بهذه اللغة على هذا الجهاز.");
    }
}
