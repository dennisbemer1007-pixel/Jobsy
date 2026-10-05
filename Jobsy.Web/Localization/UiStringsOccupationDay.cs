namespace Jobsy.Web.Localization;

public static class UiStringsOccupationDay
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string? plText = null, string? roText = null, string? arText = null)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText ?? enText;
            ro[key] = roText ?? enText;
            ar[key] = arText ?? enText;
        }

        Add("Day.Eyebrow", "Een gewone werkdag", "A typical workday", "Zwykły dzień pracy", "O zi obișnuită de lucru", "يوم عمل عادي");
        Add("Day.Invite", "Kom, ik neem je mee door een gewone werkdag.", "Come, I will walk you through a normal workday.", "Chodź, pokażę ci zwykły dzień pracy.", "Hai, te iau printr-o zi obișnuită de lucru.", "تعال، سأصحبك في يوم عمل عادي.");
        Add("Day.Title", "Een dag als {0}", "A day as {0}", "Dzień jako {0}", "O zi ca {0}", "يوم كـ {0}");
        Add("Day.Link", "Een dag in het leven als {0}", "A day in the life as {0}", "Dzień z życia jako {0}", "O zi din viața de {0}", "يوم في حياة {0}");
        Add("Day.Disclaimer", "Een typische dag. Bij elke werkgever kan het anders zijn.", "A typical day. It can be different at each employer.", "Typowy dzień. U każdego pracodawcy może być inaczej.", "O zi tipică. La fiecare angajator poate fi altfel.", "يوم نموذجي. قد يختلف الأمر عند كل صاحب عمل.");
        Add("Day.Typical", "Dit is een typische dag. Hoe de dag echt loopt, verschilt per werkgever.", "This is a typical day. The real day differs by employer.", "To typowy dzień. Prawdziwy dzień różni się u każdego pracodawcy.", "Aceasta este o zi tipică. Ziua reală diferă de la un angajator la altul.", "هذا يوم نموذجي. اليوم الحقيقي يختلف من صاحب عمل لآخر.");
        Add("Day.Morning", "Ochtend", "Morning", "Rano", "Dimineața", "الصباح");
        Add("Day.Midday", "Midden", "Middle", "Środek", "Mijloc", "الوسط");
        Add("Day.Afternoon", "Middag", "Afternoon", "Popołudnie", "După-amiaza", "بعد الظهر");
        Add("Day.Closing", "Afronden", "Wrap-up", "Zakończenie", "Încheiere", "الختام");
        Add("Day.FitTitle", "Past dit bij jou?", "Does this fit you?", "Czy to do ciebie pasuje?", "Ți se potrivește?", "هل يناسبك هذا؟");
        Add("Day.FitLead", "Kijk of dit beroep bij je past. Of ontdek een ander beroep.", "See if this occupation fits you. Or discover another one.", "Sprawdź, czy ten zawód do ciebie pasuje. Albo odkryj inny.", "Vezi dacă această meserie ți se potrivește. Sau descoperă alta.", "انظر إن كانت هذه المهنة تناسبك. أو اكتشف مهنة أخرى.");
        Add("Day.FitCta", "Bekijk je functiefit", "See your role fit", "Zobacz dopasowanie roli", "Vezi potrivirea rolului", "اطلع على ملاءمة الدور");
        Add("Day.MoreCta", "Ontdek andere beroepen", "Discover other occupations", "Odkryj inne zawody", "Descoperă alte meserii", "اكتشف مهناً أخرى");
        Add("Day.Coach", "Neem de tijd. Een gewone dag zegt nog niet alles.", "Take your time. A normal day does not say everything.", "Nie spiesz się. Zwykły dzień nie mówi wszystkiego.", "Fără grabă. O zi obișnuită nu spune tot.", "خذ وقتك. اليوم العادي لا يقول كل شيء.");
        Add("Day.Highlights", "Wat vaak terugkomt", "What often comes back", "Co często wraca", "Ce revine des", "ما يتكرر غالبًا");
        Add("Day.Varies", "Wat kan verschillen", "What can differ", "Co może się różnić", "Ce poate diferi", "ما قد يختلف");
        Add("Day.Empty", "Nog niet beschikbaar.", "Not available yet.", "Jeszcze niedostępne.", "Încă nu este disponibil.", "غير متاح بعد.");
        Add("Day.EmptyLead", "Voor dit beroep staat er nog geen werkdag klaar.", "There is no workday for this occupation yet.", "Nie ma jeszcze dnia pracy dla tego zawodu.", "Nu există încă o zi de lucru pentru această meserie.", "لا يوجد يوم عمل لهذا المهنة بعد.");
        Add("Day.Unknown", "Dit beroep kennen we niet.", "We do not know this occupation.", "Nie znamy tego zawodu.", "Nu cunoaștem această meserie.", "لا نعرف هذه المهنة.");
        Add("Day.Off", "Deze pagina staat nu uit.", "This page is off right now.", "Ta strona jest teraz wyłączona.", "Această pagină este oprită acum.", "هذه الصفحة متوقفة الآن.");
        Add("Day.Back", "Terug naar je kompas", "Back to your compass", "Wróć do kompasu", "Înapoi la busolă", "العودة إلى البوصلة");
        Add("Day.Thin", "We weten weinig over dit beroep. De dag is daarom kort.", "We know little about this occupation, so the day is short.", "Mało wiemy o tym zawodzie, więc dzień jest krótki.", "Știm puțin despre această meserie, deci ziua este scurtă.", "نعرف القليل عن هذه المهنة، لذلك اليوم قصير.");
        Add("Admin.Day.Lead", "Vul ontbrekende beroepen één keer. De Nederlandse tekst blijft de bron. Vertalingen naar de talen van Lobsy worden één keer opgeslagen. Een kandidaat ziet alleen wat hier al staat.", "Fill missing occupations once. The Dutch text stays the source. Translations into Lobsy's languages are stored once. A candidate only sees what is already stored.", "Uzupełnij brakujące zawody raz. Holenderski tekst zostaje źródłem. Tłumaczenia zapisujemy raz.", "Completează o dată meseriile lipsă. Textul olandez rămâne sursa. Traducerile se salvează o dată.", "املأ المهن الناقصة مرة واحدة. النص الهولندي يبقى المصدر. الترجمات تُحفظ مرة واحدة.");
        Add("Admin.Day.Start", "Genereer ontbrekende dagen", "Generate missing days", "Generuj brakujące dni", "Generează zilele lipsă", "ولّد الأيام الناقصة");
        Add("Admin.Day.Stop", "Stop", "Stop", "Stop", "Stop", "إيقاف");
        Add("Admin.Day.Chunk", "Volgende 10 nu", "Next 10 now", "Następne 10 teraz", "Următoarele 10 acum", "العشرة التالية الآن");
        Add("Admin.Day.Export", "Download JSON", "Download JSON", "Pobierz JSON", "Descarcă JSON", "تنزيل JSON");
        Add("Admin.Day.Import", "Importeer JSON", "Import JSON", "Importuj JSON", "Importă JSON", "استيراد JSON");
        Add("Admin.Day.Stored", "{0} van {1} beroepen hebben een Nederlandse dag. Nog {2} zonder volledige tekst of vertaling.", "{0} of {1} occupations have a Dutch day. {2} still need the full text or a translation.", "{0} z {1} zawodów ma niderlandzki dzień. Zostało {2}.", "{0} din {1} meserii au o zi în olandeză. Mai sunt {2}.", "{0} من {1} مهنة لها يوم هولندي. تبقى {2}.");
        Add("Admin.Day.Running", "Bezig: {0} nieuw, {1} mislukt.", "Running: {0} new, {1} failed.", "W toku: {0} nowych, {1} błędów.", "În curs: {0} noi, {1} eșuate.", "جارٍ: {0} جديد، {1} فشل.");
        Add("Admin.Day.Key", "OpenAI-sleutel ontbreekt. Zet OpenAI__ApiKey of de integratie-sleutel. Mistral wordt voor deze vulling niet gebruikt.", "The OpenAI key is missing. Set OpenAI__ApiKey or the integration key. This fill does not use Mistral.", "Brak klucza OpenAI.", "Lipsește cheia OpenAI.", "مفتاح OpenAI غير موجود.");
    }
}
