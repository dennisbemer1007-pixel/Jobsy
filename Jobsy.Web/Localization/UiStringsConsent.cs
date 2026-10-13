namespace Jobsy.Web.Localization;

internal static class UiStringsConsent
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        foreach (var (k, v) in Nl) nl[k] = v;
        foreach (var (k, v) in En) en[k] = v;
        foreach (var (k, v) in Pl) pl[k] = v;
        foreach (var (k, v) in Ro) ro[k] = v;
        foreach (var (k, v) in Ar) ar[k] = v;
    }

    private static readonly Dictionary<string, string> Nl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consent.Title"] = "Toestemming",
        ["Consent.HeadingNamed"] = "Toestemming voor {0}",
        ["Consent.HeadingFallback"] = "Toestemming voor je kind",
        ["Consent.LineWhat"] = "Lobsy helpt jongeren passende banen en stages te vinden.",
        ["Consent.LineTests"] = "Met toestemming mag je kind tests doen en een analyse met AI krijgen.",
        ["Consent.LineWithdraw"] = "Toestemming kun je later intrekken via privacy@lobsy.nl of de privacy-pagina.",
        ["Consent.Submit"] = "Ik geef toestemming",
        ["Consent.Success"] = "Dank je. De toestemming is gegeven.",
        ["Consent.Invalid"] = "Deze link werkt niet meer. Vraag je kind om een nieuwe aanvraag te sturen.",
        ["Consent.Seo.Title"] = "Ouderlijke toestemming",
        ["Consent.Seo.Description"] = "Bevestig ouderlijke toestemming voor Lobsy."
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consent.Title"] = "Consent",
        ["Consent.HeadingNamed"] = "Consent for {0}",
        ["Consent.HeadingFallback"] = "Consent for your child",
        ["Consent.LineWhat"] = "Lobsy helps young people find suitable jobs and internships.",
        ["Consent.LineTests"] = "With consent, your child can take tests and get an AI analysis.",
        ["Consent.LineWithdraw"] = "You can withdraw consent later via privacy@lobsy.nl or the privacy page.",
        ["Consent.Submit"] = "I give consent",
        ["Consent.Success"] = "Thank you. Consent has been given.",
        ["Consent.Invalid"] = "This link no longer works. Ask your child to send a new request.",
        ["Consent.Seo.Title"] = "Parental consent",
        ["Consent.Seo.Description"] = "Confirm parental consent for Lobsy."
    };

    private static readonly Dictionary<string, string> Pl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consent.Title"] = "Zgoda",
        ["Consent.HeadingNamed"] = "Zgoda dla {0}",
        ["Consent.HeadingFallback"] = "Zgoda dla Twojego dziecka",
        ["Consent.LineWhat"] = "Lobsy pomaga młodym znaleźć odpowiednią pracę i staże.",
        ["Consent.LineTests"] = "Za zgodą dziecko może robić testy i dostać analizę AI.",
        ["Consent.LineWithdraw"] = "Zgodę możesz później wycofać przez privacy@lobsy.nl lub stronę prywatności.",
        ["Consent.Submit"] = "Wyrażam zgodę",
        ["Consent.Success"] = "Dziękujemy. Zgoda została udzielona.",
        ["Consent.Invalid"] = "Ten link już nie działa. Poproś dziecko o nową prośbę.",
        ["Consent.Seo.Title"] = "Zgoda rodzicielska",
        ["Consent.Seo.Description"] = "Potwierdź zgodę rodzicielską dla Lobsy."
    };

    private static readonly Dictionary<string, string> Ro = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consent.Title"] = "Consimțământ",
        ["Consent.HeadingNamed"] = "Consimțământ pentru {0}",
        ["Consent.HeadingFallback"] = "Consimțământ pentru copilul tău",
        ["Consent.LineWhat"] = "Lobsy îi ajută pe tineri să găsească joburi și stagii potrivite.",
        ["Consent.LineTests"] = "Cu consimțământ, copilul poate face teste și primi o analiză AI.",
        ["Consent.LineWithdraw"] = "Poți retrage consimțământul ulterior via privacy@lobsy.nl sau pagina de confidențialitate.",
        ["Consent.Submit"] = "Dau consimțământul",
        ["Consent.Success"] = "Mulțumim. Consimțământul a fost acordat.",
        ["Consent.Invalid"] = "Acest link nu mai funcționează. Cere copilului o nouă solicitare.",
        ["Consent.Seo.Title"] = "Consimțământ parental",
        ["Consent.Seo.Description"] = "Confirmă consimțământul parental pentru Lobsy."
    };

    private static readonly Dictionary<string, string> Ar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Consent.Title"] = "الموافقة",
        ["Consent.HeadingNamed"] = "موافقة لـ {0}",
        ["Consent.HeadingFallback"] = "موافقة لطفلك",
        ["Consent.LineWhat"] = "تساعد Lobsy الشباب على إيجاد وظائف وتدريبات مناسبة.",
        ["Consent.LineTests"] = "بالموافقة يمكن لطفلك إجراء اختبارات والحصول على تحليل بالذكاء الاصطناعي.",
        ["Consent.LineWithdraw"] = "يمكنك سحب الموافقة لاحقًا عبر privacy@lobsy.nl أو صفحة الخصوصية.",
        ["Consent.Submit"] = "أوافق",
        ["Consent.Success"] = "شكرًا. تم منح الموافقة.",
        ["Consent.Invalid"] = "هذا الرابط لم يعد يعمل. اطلب من طفلك إرسال طلب جديد.",
        ["Consent.Seo.Title"] = "موافقة الوالدين",
        ["Consent.Seo.Description"] = "أكد موافقة الوالدين لـ Lobsy."
    };
}
