namespace Jobsy.Web.Localization;

internal static class UiStringsResearch
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, Nl());
        Merge(en, En());
        Merge(pl, Pl());
        Merge(ro, Ro());
        Merge(ar, Ar());
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> extra)
    {
        foreach (var (key, value) in extra)
        {
            target[key] = value;
        }
    }

    private static Dictionary<string, string> Nl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Research.Consent.Label"] = "Mijn antwoorden mogen geanonimiseerd gebruikt worden voor wetenschappelijk onderzoek naar de kwaliteit van de tests.",
        ["Research.Consent.Lead"] = "Dit is vrijwillig en los van je account, sollicitaties of talentpool. Je kunt dit later intrekken in je privacy-instellingen.",
        ["Research.Consent.InfoLink"] = "Meer over het onderzoek",
        ["Research.Consent.Accept"] = "Opslaan en verder",
        ["Research.Consent.Skip"] = "Nee, liever niet",
        ["Research.Consent.Revoke"] = "Onderzoekstoestemming intrekken",
        ["Research.Demo.Title"] = "Optioneel: help ons de tests eerlijk te maken",
        ["Research.Demo.Age"] = "Leeftijdsgroep",
        ["Research.Demo.Gender"] = "Gender",
        ["Research.Demo.Education"] = "Opleidingsniveau",
        ["Research.Demo.NativeLang"] = "Moedertaal",
        ["Research.Demo.TestLang"] = "Taal van de test",
        ["Research.Demo.PreferNot"] = "Zeg ik liever niet",
        ["Research.Pilot.Unclear"] = "Deze vraag was onduidelijk",
        ["Research.Pilot.Comment"] = "Opmerking over de test (optioneel)",
        ["Research.Admin.Title"] = "Testonderzoek (COTAN)",
        ["Research.Admin.Enable"] = "Onderzoeksdeelname inschakelen",
        ["Research.Admin.Export"] = "Anonieme CSV export",
        ["Research.Admin.Target"] = "Doel N=300 per test/versie/taal",
        ["Nav.ResearchAdmin"] = "Testonderzoek",
        ["Research.Info.Title"] = "Onderzoek naar onze tests",
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Research.Consent.Label"] = "My answers may be used anonymously for scientific research into the quality of the tests.",
        ["Research.Consent.Lead"] = "This is optional and separate from your account, applications or talent pool. You can withdraw later in privacy settings.",
        ["Research.Consent.InfoLink"] = "More about the research",
        ["Research.Consent.Accept"] = "Save and continue",
        ["Research.Consent.Skip"] = "No thanks",
        ["Research.Consent.Revoke"] = "Withdraw research consent",
        ["Research.Demo.Title"] = "Optional: help us keep tests fair",
        ["Research.Demo.Age"] = "Age band",
        ["Research.Demo.Gender"] = "Gender",
        ["Research.Demo.Education"] = "Education level",
        ["Research.Demo.NativeLang"] = "Native language",
        ["Research.Demo.TestLang"] = "Test language",
        ["Research.Demo.PreferNot"] = "Prefer not to say",
        ["Research.Pilot.Unclear"] = "This question was unclear",
        ["Research.Pilot.Comment"] = "Comment about the test (optional)",
        ["Research.Admin.Title"] = "Test research (COTAN)",
        ["Research.Admin.Enable"] = "Enable research participation",
        ["Research.Admin.Export"] = "Anonymous CSV export",
        ["Research.Admin.Target"] = "Target N=300 per test/version/language",
        ["Nav.ResearchAdmin"] = "Test research",
        ["Research.Info.Title"] = "Research on our tests",
    };

    private static Dictionary<string, string> Pl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Research.Consent.Label"] = "Moje odpowiedzi mogą być anonimowo wykorzystywane do badań naukowych nad jakością testów.",
        ["Research.Consent.Lead"] = "To dobrowolne i oddzielne od konta, aplikacji i puli talentów. Możesz wycofać zgodę w ustawieniach prywatności.",
        ["Research.Consent.InfoLink"] = "Więcej o badaniu",
        ["Research.Consent.Accept"] = "Zapisz i dalej",
        ["Research.Consent.Skip"] = "Nie, dziękuję",
        ["Research.Consent.Revoke"] = "Wycofaj zgodę na badania",
        ["Research.Demo.Title"] = "Opcjonalnie: pomóż nam utrzymać testy sprawiedliwe",
        ["Research.Demo.Age"] = "Przedział wiekowy",
        ["Research.Demo.Gender"] = "Płeć",
        ["Research.Demo.Education"] = "Poziom wykształcenia",
        ["Research.Demo.NativeLang"] = "Język ojczysty",
        ["Research.Demo.TestLang"] = "Język testu",
        ["Research.Demo.PreferNot"] = "Wolę nie podawać",
        ["Research.Pilot.Unclear"] = "To pytanie było niejasne",
        ["Research.Pilot.Comment"] = "Uwagi o teście (opcjonalnie)",
        ["Research.Admin.Title"] = "Badania testów (COTAN)",
        ["Research.Admin.Enable"] = "Włącz udział w badaniu",
        ["Research.Admin.Export"] = "Anonimowy eksport CSV",
        ["Research.Admin.Target"] = "Cel N=300 na test/wersję/język",
        ["Nav.ResearchAdmin"] = "Badania testów",
        ["Research.Info.Title"] = "Badania naszych testów",
    };

    private static Dictionary<string, string> Ro() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Research.Consent.Label"] = "Răspunsurile mele pot fi folosite anonim pentru cercetare științifică privind calitatea testelor.",
        ["Research.Consent.Lead"] = "Este opțional și separat de cont, candidaturi sau talent pool. Poți retrage consimțământul din setările de confidențialitate.",
        ["Research.Consent.InfoLink"] = "Mai multe despre cercetare",
        ["Research.Consent.Accept"] = "Salvează și continuă",
        ["Research.Consent.Skip"] = "Nu, mulțumesc",
        ["Research.Consent.Revoke"] = "Retrage consimțământul pentru cercetare",
        ["Research.Demo.Title"] = "Opțional: ajută-ne să păstrăm testele echitabile",
        ["Research.Demo.Age"] = "Grupă de vârstă",
        ["Research.Demo.Gender"] = "Gen",
        ["Research.Demo.Education"] = "Nivel de educație",
        ["Research.Demo.NativeLang"] = "Limba maternă",
        ["Research.Demo.TestLang"] = "Limba testului",
        ["Research.Demo.PreferNot"] = "Prefer să nu spun",
        ["Research.Pilot.Unclear"] = "Întrebarea aceasta nu era clară",
        ["Research.Pilot.Comment"] = "Comentariu despre test (opțional)",
        ["Research.Admin.Title"] = "Cercetare teste (COTAN)",
        ["Research.Admin.Enable"] = "Activează participarea la cercetare",
        ["Research.Admin.Export"] = "Export CSV anonim",
        ["Research.Admin.Target"] = "Țintă N=300 per test/versiune/limbă",
        ["Nav.ResearchAdmin"] = "Cercetare teste",
        ["Research.Info.Title"] = "Cercetare despre testele noastre",
    };

    private static Dictionary<string, string> Ar() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Research.Consent.Label"] = "يُمكن استخدام إجاباتي بشكل مجهول للبحث العلمي حول جودة الاختبارات.",
        ["Research.Consent.Lead"] = "هذا اختiاري ومنفصل عن حسابك أو طلباتك أو مجموعة المواهب. يمكنك سحب الموافقة لاحقاً من إعدادات الخصوصية.",
        ["Research.Consent.InfoLink"] = "المزيد عن البحث",
        ["Research.Consent.Accept"] = "حفظ ومتابعة",
        ["Research.Consent.Skip"] = "لا شكراً",
        ["Research.Consent.Revoke"] = "سحب موافقة البحث",
        ["Research.Demo.Title"] = "اختياري: ساعدنا على عدالة الاختبارات",
        ["Research.Demo.Age"] = "الفئة العمرية",
        ["Research.Demo.Gender"] = "الجنس",
        ["Research.Demo.Education"] = "المستوى التعليمي",
        ["Research.Demo.NativeLang"] = "اللغة الأم",
        ["Research.Demo.TestLang"] = "لغة الاختبار",
        ["Research.Demo.PreferNot"] = "أفضل عدم الإفصاح",
        ["Research.Pilot.Unclear"] = "كان هذا السؤال غير واضح",
        ["Research.Pilot.Comment"] = "ملاحظة عن الاختبار (اختياري)",
        ["Research.Admin.Title"] = "بحث الاختبارات (COTAN)",
        ["Research.Admin.Enable"] = "تفعيل المشاركة في البحث",
        ["Research.Admin.Export"] = "تصدير CSV مجهول",
        ["Research.Admin.Target"] = "الهدف N=300 لكل اختبار/إصدار/لغة",
        ["Nav.ResearchAdmin"] = "بحث الاختبارات",
        ["Research.Info.Title"] = "البحث حول اختباراتنا",
    };
}
