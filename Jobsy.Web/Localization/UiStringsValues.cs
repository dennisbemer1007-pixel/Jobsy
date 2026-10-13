using Jobsy.Core.Rules;

namespace Jobsy.Web.Localization;

internal static class UiStringsValues
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
        ["ValuesScan.Title"] = "Waarden op werk",
        ["ValuesScan.Lead"] = "Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",

        ["ValuesScan.Cat.Autonomy"] = DimensionLabels.For(SchwartzValuesCatalog.Autonomy),
        ["ValuesScan.Cat.Autonomy.Hint"] = "Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection"] = DimensionLabels.For(SchwartzValuesCatalog.Connection),
        ["ValuesScan.Cat.Connection.Hint"] = "Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement"] = DimensionLabels.For(SchwartzValuesCatalog.Achievement),
        ["ValuesScan.Cat.Achievement.Hint"] = "Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability"] = DimensionLabels.For(SchwartzValuesCatalog.Stability),
        ["ValuesScan.Cat.Stability.Hint"] = "Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact"] = DimensionLabels.For(SchwartzValuesCatalog.Impact),
        ["ValuesScan.Cat.Impact.Hint"] = "Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",

        ["ValuesScan.Q01"] = "Ik wil zelf kunnen bepalen hoe ik mijn werk aanpak.",
        ["ValuesScan.Q02"] = "Nieuwe uitdagingen op het werk motiveren mij.",
        ["ValuesScan.Q03"] = "Ik werk het best als iemand precies zegt wat ik moet doen.",
        ["ValuesScan.Q04"] = "Ik zoek graag nieuwe manieren om mijn werk te doen.",
        ["ValuesScan.Q05"] = "Ik voel me prettiger bij vaste routines dan bij veel wisseling.",
        ["ValuesScan.Q06"] = "Ik help collega's graag, ook buiten mijn eigen taken.",
        ["ValuesScan.Q07"] = "Een warme, behulpzame sfeer op het werk is belangrijk voor mij.",
        ["ValuesScan.Q08"] = "Ik houd me liever afzijdig van persoonlijke gesprekken met collega's.",
        ["ValuesScan.Q09"] = "Ik let op hoe het met mensen om me heen gaat.",
        ["ValuesScan.Q10"] = "Teamgevoel vind ik minder belangrijk dan mijn eigen resultaat.",
        ["ValuesScan.Q11"] = "Ik wil zien dat mijn inzet meetbaar verschil maakt.",
        ["ValuesScan.Q12"] = "Ik stel mezelf doelen en werk er actief naar toe.",
        ["ValuesScan.Q13"] = "Ik vind het prima om gemiddeld te presteren zolang het werk af is.",
        ["ValuesScan.Q14"] = "Ik ben trots als ik normen of targets overtref.",
        ["ValuesScan.Q15"] = "Ik zoek geen extra verantwoordelijkheid voor belangrijke resultaten.",
        ["ValuesScan.Q16"] = "Duidelijke afspraken over uren en taken geven mij rust.",
        ["ValuesScan.Q17"] = "Ik werk graag volgens vaste, bewezen procedures.",
        ["ValuesScan.Q18"] = "Regels en protocollen voelen voor mij vooral belemmerend.",
        ["ValuesScan.Q19"] = "Ik zoek werk met langdurige zekerheid en stabiliteit.",
        ["ValuesScan.Q20"] = "Ik raak onrustig als mijn taken te voorspelbaar worden.",
        ["ValuesScan.Q21"] = "Ik wil dat mijn werk iets goeds doet voor mens of milieu.",
        ["ValuesScan.Q22"] = "Ik kies liever voor een organisatie met eerlijke, duurzame praktijken.",
        ["ValuesScan.Q23"] = "Het maatschappelijke doel van mijn werk is mij niet zo belangrijk.",
        ["ValuesScan.Q24"] = "Ik let op verspilling en probeer die op het werk te verminderen.",
        ["ValuesScan.Q25"] = "Ik focus op mijn eigen taken, niet op bredere maatschappelijke thema's.",

        ["ValuesScan.SavedComplete"] = "Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "Gebaseerd op het Schwartz Value Model",

        ["Deep.ValuesTitle"] = "Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "Honderdvijftig stellingen over hoe je werkt, wat je leuk vindt, de sfeer op het werk en wat jou drijft. Pauzeren mag — je hervat later waar je was.",

        ["Kompas.TabValues"] = "Waarden",
        ["Kompas.ValuesLead"] = "Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "Uitgebreide waardentest afgerond",
        ["Kompas.ValuesDeepReady"] = "Uitgebreide waardentest ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "Uitgebreide test (150 vragen) nog niet ontgrendeld",
    };

    private static Dictionary<string, string> En()
    {
        var map = Nl();
        map["ValuesScan.Title"] = "Work values";
        map["ValuesScan.Lead"] = "Twenty-five short statements (about 4 minutes) on what matters to you at work — from autonomy to stability and social impact.";
        map["ValuesScan.ScienceNote"] = "Scientific model: Schwartz Value Model (workplace drivers: autonomy, connection, achievement, stability, impact). Not a diagnosis — a clear starting point for values fit in matching.";
        map["ValuesScan.PrivacyNote"] = "Your answers stay in your account. Employers do not see raw responses. Export and delete via My data. More in the";
        map["ValuesScan.Cat.Autonomy"] = "Autonomy & challenge";
        map["ValuesScan.Cat.Autonomy.Hint"] = "How important is it to choose how you work and try new things?";
        map["ValuesScan.Cat.Connection"] = "Connection & care";
        map["ValuesScan.Cat.Connection.Hint"] = "How important are warm relationships with colleagues, customers, and a helpful atmosphere?";
        map["ValuesScan.Cat.Achievement"] = "Achievement & growth";
        map["ValuesScan.Cat.Achievement.Hint"] = "How strongly do you want visible results, goals, and self-improvement?";
        map["ValuesScan.Cat.Stability"] = "Stability & tradition";
        map["ValuesScan.Cat.Stability.Hint"] = "How important are clear agreements, safety, and predictability?";
        map["ValuesScan.Cat.Impact"] = "Impact & fairness";
        map["ValuesScan.Cat.Impact.Hint"] = "How important is doing good for people and the environment through your work?";
        map["ValuesScan.SavedComplete"] = "Values scan saved. Your drivers count in matching and Who am I.";
        map["ValuesScan.Retake"] = "Retake free test";
        map["ProfileHub.ValuesScience"] = "Based on the Schwartz Value Model";
        map["Deep.ValuesTitle"] = "Values & drivers (deep analysis)";
        map["Deep.ValuesLead"] = "One hundred fifty statements on competencies, interests, culture fit and drivers. Pause anytime — resume later where you left off.";
        map["Kompas.TabValues"] = "Values";
        map["Kompas.ValuesLead"] = "What matters to you at work: autonomy, connection, achievement, stability and impact.";
        map["Kompas.ValuesDeepDone"] = "Extended values test completed";
        map["Kompas.ValuesDeepReady"] = "Extended values test unlocked — continue where you left off";
        map["Kompas.ValuesDeepLocked"] = "Extended test (150 questions) not unlocked yet";
        map["ValuesScan.Q01"] = "I want to decide myself how I approach my work.";
        map["ValuesScan.Q02"] = "New challenges at work motivate me.";
        map["ValuesScan.Q03"] = "I work best when someone tells me exactly what to do.";
        map["ValuesScan.Q04"] = "I like looking for new ways to do my work.";
        map["ValuesScan.Q05"] = "I feel more comfortable with fixed routines than with lots of change.";
        map["ValuesScan.Q06"] = "I gladly help colleagues, even outside my own tasks.";
        map["ValuesScan.Q07"] = "A warm, helpful atmosphere at work matters to me.";
        map["ValuesScan.Q08"] = "I prefer to stay out of personal talks with colleagues.";
        map["ValuesScan.Q09"] = "I notice how the people around me are doing.";
        map["ValuesScan.Q10"] = "Team spirit matters less to me than my own result.";
        map["ValuesScan.Q11"] = "I want to see that my effort makes a measurable difference.";
        map["ValuesScan.Q12"] = "I set goals for myself and work toward them actively.";
        map["ValuesScan.Q13"] = "I am fine with average performance as long as the work is done.";
        map["ValuesScan.Q14"] = "I am proud when I beat norms or targets.";
        map["ValuesScan.Q15"] = "I do not seek extra responsibility for important results.";
        map["ValuesScan.Q16"] = "Clear agreements about hours and tasks give me calm.";
        map["ValuesScan.Q17"] = "I like working with fixed, proven procedures.";
        map["ValuesScan.Q18"] = "Rules and protocols mostly feel limiting to me.";
        map["ValuesScan.Q19"] = "I look for work with long-term certainty and stability.";
        map["ValuesScan.Q20"] = "I get restless when my tasks become too predictable.";
        map["ValuesScan.Q21"] = "I want my work to do something good for people or the environment.";
        map["ValuesScan.Q22"] = "I prefer an organisation with fair, sustainable practices.";
        map["ValuesScan.Q23"] = "The social purpose of my work is not so important to me.";
        map["ValuesScan.Q24"] = "I watch for waste and try to reduce it at work.";
        map["ValuesScan.Q25"] = "I focus on my own tasks, not on broader social themes.";
        return map;
    }

    private static Dictionary<string, string> Pl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "Autonomia i wyzwanie",
        ["ValuesScan.Cat.Connection"] = "Więź i troska",
        ["ValuesScan.Cat.Achievement"] = "Osiągnięcia i rozwój",
        ["ValuesScan.Cat.Stability"] = "Stabilność i tradycja",
        ["ValuesScan.Cat.Impact"] = "Wpływ i sprawiedliwość",
        ["ValuesScan.Title"] = "PL: Waarden op werk",
        ["ValuesScan.Lead"] = "PL: Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "PL: Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "PL: Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "PL: Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "PL: Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "PL: Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "PL: Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "PL: Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "Chcę sam decydować, jak podchodzę do pracy.",
        ["ValuesScan.Q02"] = "Nowe wyzwania w pracy mnie motywują.",
        ["ValuesScan.Q03"] = "Najlepiej pracuję, gdy ktoś mówi mi dokładnie, co mam zrobić.",
        ["ValuesScan.Q04"] = "Lubię szukać nowych sposobów wykonywania pracy.",
        ["ValuesScan.Q05"] = "Lepiej czuję się przy stałych rutynach niż przy dużej zmienności.",
        ["ValuesScan.Q06"] = "Chętnie pomagam kolegom, także poza własnymi zadaniami.",
        ["ValuesScan.Q07"] = "Ciepła, pomocna atmosfera w pracy jest dla mnie ważna.",
        ["ValuesScan.Q08"] = "Wolę nie wchodzić w osobiste rozmowy z kolegami.",
        ["ValuesScan.Q09"] = "Zauważam, jak mają się ludzie wokół mnie.",
        ["ValuesScan.Q10"] = "Duch zespołu jest dla mnie mniej ważny niż własny wynik.",
        ["ValuesScan.Q11"] = "Chcę widzieć, że mój wysiłek daje mierzalną różnicę.",
        ["ValuesScan.Q12"] = "Stawiam sobie cele i aktywnie do nich dążę.",
        ["ValuesScan.Q13"] = "Wystarcza mi przeciętny wynik, byle praca była zrobiona.",
        ["ValuesScan.Q14"] = "Jestem dumny, gdy przekraczam normy albo cele.",
        ["ValuesScan.Q15"] = "Nie szukam dodatkowej odpowiedzialności za ważne wyniki.",
        ["ValuesScan.Q16"] = "Jasne ustalenia co do godzin i zadań dają mi spokój.",
        ["ValuesScan.Q17"] = "Lubię pracować według stałych, sprawdzonych procedur.",
        ["ValuesScan.Q18"] = "Zasady i protokoły najczęściej mnie ograniczają.",
        ["ValuesScan.Q19"] = "Szukam pracy z długotrwałą pewnością i stabilnością.",
        ["ValuesScan.Q20"] = "Robię się niespokojny, gdy zadania stają się zbyt przewidywalne.",
        ["ValuesScan.Q21"] = "Chcę, żeby moja praca robiła coś dobrego dla ludzi albo środowiska.",
        ["ValuesScan.Q22"] = "Wolę organizację z uczciwymi, zrównoważonymi praktykami.",
        ["ValuesScan.Q23"] = "Społeczny cel mojej pracy nie jest dla mnie tak ważny.",
        ["ValuesScan.Q24"] = "Zwracam uwagę na marnotrawstwo i staram się je ograniczać w pracy.",
        ["ValuesScan.Q25"] = "Skupiam się na własnych zadaniach, nie na szerszych tematach społecznych.",
        ["ValuesScan.SavedComplete"] = "PL: Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "PL: Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "PL: Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "PL: Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "PL: Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "PL: Waarden",
        ["Kompas.ValuesLead"] = "PL: Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "PL: Uitgebreide waardentest afgerond",
        ["Kompas.ValuesDeepReady"] = "PL: Uitgebreide waardentest ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "PL: Uitgebreide test (150 vragen) nog niet ontgrendeld",
    };

    private static Dictionary<string, string> Ro() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "Autonomie și provocare",
        ["ValuesScan.Cat.Connection"] = "Conexiune și grijă",
        ["ValuesScan.Cat.Achievement"] = "Realizare și creștere",
        ["ValuesScan.Cat.Stability"] = "Stabilitate și tradiție",
        ["ValuesScan.Cat.Impact"] = "Impact și corectitudine",
        ["ValuesScan.Title"] = "RO: Waarden op werk",
        ["ValuesScan.Lead"] = "RO: Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "RO: Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "RO: Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "RO: Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "RO: Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "RO: Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "RO: Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "RO: Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "Vreau să decid eu cum îmi abordez munca.",
        ["ValuesScan.Q02"] = "Provocările noi la muncă mă motivează.",
        ["ValuesScan.Q03"] = "Lucrez cel mai bine când cineva îmi spune exact ce să fac.",
        ["ValuesScan.Q04"] = "Îmi place să caut feluri noi de a-mi face treaba.",
        ["ValuesScan.Q05"] = "Mă simt mai confortabil cu rutine fixe decât cu multă schimbare.",
        ["ValuesScan.Q06"] = "Ajut cu plăcere colegii, chiar și în afara sarcinilor mele.",
        ["ValuesScan.Q07"] = "O atmosferă caldă și de ajutor la muncă contează pentru mine.",
        ["ValuesScan.Q08"] = "Prefer să stau departe de discuțiile personale cu colegii.",
        ["ValuesScan.Q09"] = "Observ cum le este oamenilor din jurul meu.",
        ["ValuesScan.Q10"] = "Spiritul de echipă contează mai puțin pentru mine decât rezultatul meu.",
        ["ValuesScan.Q11"] = "Vreau să văd că efortul meu face o diferență măsurabilă.",
        ["ValuesScan.Q12"] = "Îmi setez obiective și lucrez activ spre ele.",
        ["ValuesScan.Q13"] = "Îmi este bine cu o performanță medie, cât timp treaba e făcută.",
        ["ValuesScan.Q14"] = "Sunt mândru când depășesc norme sau ținte.",
        ["ValuesScan.Q15"] = "Nu caut responsabilitate în plus pentru rezultate importante.",
        ["ValuesScan.Q16"] = "Înțelegerile clare despre ore și sarcini îmi dau liniște.",
        ["ValuesScan.Q17"] = "Îmi place să lucrez după proceduri fixe, dovedite.",
        ["ValuesScan.Q18"] = "Regulile și protocoalele mi se par mai ales limitative.",
        ["ValuesScan.Q19"] = "Caut muncă cu certitudine și stabilitate pe termen lung.",
        ["ValuesScan.Q20"] = "Devin neliniștit când sarcinile devin prea previzibile.",
        ["ValuesScan.Q21"] = "Vreau ca munca mea să facă ceva bun pentru oameni sau mediu.",
        ["ValuesScan.Q22"] = "Prefer o organizație cu practici corecte și sustenabile.",
        ["ValuesScan.Q23"] = "Scopul social al muncii mele nu este atât de important pentru mine.",
        ["ValuesScan.Q24"] = "Sunt atent la risipă și încerc să o reduc la muncă.",
        ["ValuesScan.Q25"] = "Mă concentrez pe sarcinile mele, nu pe teme sociale mai largi.",
        ["ValuesScan.SavedComplete"] = "RO: Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "RO: Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "RO: Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "RO: Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "RO: Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "RO: Waarden",
        ["Kompas.ValuesLead"] = "RO: Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "RO: Uitgebreide waardentest afgerond",
        ["Kompas.ValuesDeepReady"] = "RO: Uitgebreide waardentest ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "RO: Uitgebreide test (150 vragen) nog niet ontgrendeld",
    };

    private static Dictionary<string, string> Ar() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "الاستقلالية والتحدي",
        ["ValuesScan.Cat.Connection"] = "الترابط والرعاية",
        ["ValuesScan.Cat.Achievement"] = "الإنجاز والنمو",
        ["ValuesScan.Cat.Stability"] = "الاستقرار والتقاليد",
        ["ValuesScan.Cat.Impact"] = "الأثر والعدالة",
        ["ValuesScan.Title"] = "‏Waarden op werk",
        ["ValuesScan.Lead"] = "‏Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "‏Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "‏Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "‏Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "‏Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "‏Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "‏Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "‏Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "أريد أن أقرر بنفسي كيف أتعامل مع عملي.",
        ["ValuesScan.Q02"] = "التحديات الجديدة في العمل تحفّزني.",
        ["ValuesScan.Q03"] = "أعمل بأفضل شكل عندما يخبرني أحد بالضبط ماذا أفعل.",
        ["ValuesScan.Q04"] = "أحب البحث عن طرق جديدة لأداء عملي.",
        ["ValuesScan.Q05"] = "أشعر براحة أكبر مع الروتين الثابت من التغيير الكثير.",
        ["ValuesScan.Q06"] = "أساعد زملائي بسرور، حتى خارج مهامي.",
        ["ValuesScan.Q07"] = "الجو الدافئ والمتعاون في العمل مهم لي.",
        ["ValuesScan.Q08"] = "أفضل الابتعاد عن الأحاديث الشخصية مع الزملاء.",
        ["ValuesScan.Q09"] = "أنتبه إلى حال الناس من حولي.",
        ["ValuesScan.Q10"] = "روح الفريق أقل أهمية لي من نتيجتي الخاصة.",
        ["ValuesScan.Q11"] = "أريد أن أرى أن جهدي يصنع فرقًا يمكن قياسه.",
        ["ValuesScan.Q12"] = "أضع لنفسي أهدافًا وأعمل بنشاط للوصول إليها.",
        ["ValuesScan.Q13"] = "لا بأس لدي بأداء متوسط ما دام العمل منجزًا.",
        ["ValuesScan.Q14"] = "أشعر بالفخر عندما أتجاوز المعايير أو الأهداف.",
        ["ValuesScan.Q15"] = "لا أبحث عن مسؤولية إضافية للنتائج المهمة.",
        ["ValuesScan.Q16"] = "الاتفاقات الواضحة حول الساعات والمهام تمنحني هدوءًا.",
        ["ValuesScan.Q17"] = "أحب العمل وفق إجراءات ثابتة ومجرّبة.",
        ["ValuesScan.Q18"] = "القواعد والبروتوكولات تقيّدني في الغالب.",
        ["ValuesScan.Q19"] = "أبحث عن عمل فيه أمان واستقرار طويل الأمد.",
        ["ValuesScan.Q20"] = "أصبح قلقًا عندما تصبح مهامي متوقعة أكثر من اللازم.",
        ["ValuesScan.Q21"] = "أريد أن يفعل عملي شيئًا جيدًا للناس أو للبيئة.",
        ["ValuesScan.Q22"] = "أفضل منظمة ذات ممارسات عادلة ومستدامة.",
        ["ValuesScan.Q23"] = "الهدف المجتمعي لعملي ليس مهمًا لي إلى هذا الحد.",
        ["ValuesScan.Q24"] = "أنتبه للهدر وأحاول تقليله في العمل.",
        ["ValuesScan.Q25"] = "أركز على مهامي، لا على موضوعات مجتمعية أوسع.",
        ["ValuesScan.SavedComplete"] = "‏Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "‏Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "‏Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "‏Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "‏Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "‏Waarden",
        ["Kompas.ValuesLead"] = "‏Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "‏Uitgebreide waardentest afgerond",
        ["Kompas.ValuesDeepReady"] = "‏Uitgebreide waardentest ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "‏Uitgebreide test (150 vragen) nog niet ontgrendeld",
    };
}
