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
        Add("Day.Tasks", "Wat je vaak doet", "What you often do", "Co często robisz", "Ce faci des", "ما تفعله غالبًا");
        Add("Day.Skills", "Wat dit werk vraagt", "What this work asks", "Czego wymaga ta praca", "Ce cere această muncă", "ما يطلبه هذا العمل");
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
        Add("Admin.Day.Stop", "Stoppen", "Stop", "Zatrzymaj", "Oprește", "إيقاف");
        Add("Admin.Day.Chunk", "Volgende 10 nu", "Next 10 now", "Następne 10 teraz", "Următoarele 10 acum", "العشرة التالية الآن");
        Add("Admin.Day.Probe", "Test 1 beroep", "Test 1 occupation", "Test 1 zawód", "Testează 1 meserie", "اختبر مهنة واحدة");
        Add("Admin.Day.Pick", "Beroepen voor een proef", "Occupations for a trial", "Zawody do próby", "Meserii pentru o probă", "مهن للتجربة");
        Add("Admin.Day.PickLead", "Plak namen of ESCO-ids, één per regel. Bijvoorbeeld leraar basisonderwijs, kok of schilder.", "Paste names or ESCO ids, one per line. For example primary school teacher, cook or painter.", "Wklej nazwy lub identyfikatory ESCO, jedna na wiersz.", "Lipește nume sau id-uri ESCO, câte unul pe rând.", "ألصق الأسماء أو معرّفات ESCO، واحداً في كل سطر.");
        Add("Admin.Day.Started", "De vulling loopt op de achtergrond. Hier zie je het resultaat.", "The fill runs in the background. The result shows here.", "Uzupełnianie działa w tle. Wynik widać tutaj.", "Completarea rulează în fundal. Rezultatul apare aici.", "التعبئة تعمل في الخلفية. النتيجة تظهر هنا.");
        Add("Admin.Day.Busy", "Er loopt al een vulling.", "A fill is already running.", "Uzupełnianie już trwa.", "O completare rulează deja.", "هناك تعبئة قيد التشغيل.");
        Add("Admin.Day.Summary", "{0} gelukt, {1} mislukt.", "{0} succeeded, {1} failed.", "{0} udane, {1} nieudane.", "{0} reușite, {1} eșuate.", "{0} نجح، {1} فشل.");
        Add("Admin.Day.Timeout", "OpenAI reageerde niet op tijd.", "OpenAI did not answer in time.", "OpenAI nie odpowiedziało na czas.", "OpenAI nu a răspuns la timp.", "لم يرد OpenAI في الوقت المناسب.");
        Add("Admin.Day.KeyInvalid", "OpenAI weigert de sleutel. Controleer de OpenAI-sleutel. Deze vulling gebruikt geen Mistral.", "OpenAI rejects the key. Check the OpenAI key. This fill does not use Mistral.", "OpenAI odrzuca klucz. Sprawdź klucz OpenAI. To uzupełnianie nie używa Mistral.", "OpenAI respinge cheia. Verifică cheia OpenAI. Această completare nu folosește Mistral.", "يرفض OpenAI المفتاح. تحقق من مفتاح OpenAI. هذا الملء لا يستخدم Mistral.");
        Add("Admin.Day.Http", "OpenAI gaf een fout ({0}).", "OpenAI returned an error ({0}).", "OpenAI zwróciło błąd ({0}).", "OpenAI a dat o eroare ({0}).", "أعاد OpenAI خطأ ({0}).");
        Add("Admin.Day.PickEmpty", "Kies eerst één beroep.", "Choose one occupation first.", "Najpierw wybierz jeden zawód.", "Alege mai întâi o meserie.", "اختر مهنة واحدة أولاً.");
        Add("Admin.Day.ProbeOk", "{0}: OpenAI antwoordde.", "{0}: OpenAI answered.", "{0}: OpenAI odpowiedziało.", "{0}: OpenAI a răspuns.", "{0}: أجاب OpenAI.");
        Add("Admin.Day.UnknownJob", "Dit beroep kennen we niet: {0}", "We do not know this occupation: {0}", "Nie znamy tego zawodu: {0}", "Nu cunoaștem această meserie: {0}", "لا نعرف هذه المهنة: {0}");
        Add("Admin.Day.Export", "JSON downloaden", "Download JSON", "Pobierz JSON", "Descarcă JSON", "تنزيل JSON");
        Add("Admin.Day.Import", "Importeer JSON", "Import JSON", "Importuj JSON", "Importă JSON", "استيراد JSON");
        Add("Admin.Day.Stored", "{0} van {1} beroepen hebben een Nederlandse dag. Nog {2} zonder volledige tekst of vertaling.", "{0} of {1} occupations have a Dutch day. {2} still need the full text or a translation.", "{0} z {1} zawodów ma niderlandzki dzień. Zostało {2}.", "{0} din {1} meserii au o zi în olandeză. Mai sunt {2}.", "{0} من {1} مهنة لها يوم هولندي. تبقى {2}.");
        Add("Admin.Day.Running", "Bezig: {0} nieuw, {1} mislukt.", "Running: {0} new, {1} failed.", "W toku: {0} nowych, {1} błędów.", "În curs: {0} noi, {1} eșuate.", "جارٍ: {0} جديد، {1} فشل.");
        Add("Admin.Day.Key", "OpenAI-sleutel ontbreekt. Zet OpenAI__ApiKey of de integratie-sleutel. Mistral wordt voor deze vulling niet gebruikt.", "The OpenAI key is missing. Set OpenAI__ApiKey or the integration key. This fill does not use Mistral.", "Brak klucza OpenAI.", "Lipsește cheia OpenAI.", "مفتاح OpenAI غير موجود.");
    }
}
