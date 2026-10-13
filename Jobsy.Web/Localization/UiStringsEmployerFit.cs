namespace Jobsy.Web.Localization;

public static class UiStringsEmployerFit
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

        Add("EmployerFit.Team.Title",
            "Zo werken wij (optioneel)",
            "How we work (optional)",
            "Jak pracujemy (opcjonalnie)",
            "Cum lucrăm (opțional)",
            "كيف نعمل (اختياري)");
        Add("EmployerFit.Team.Lead",
            "Zes korte stellingen zijn genoeg; uitgebreider kan later.",
            "Six short statements are enough; you can add more later.",
            "Sześć krótkich stwierdzeń wystarczy.",
            "Șase enunțuri scurte sunt suficiente.",
            "ست عبارات قصيرة كافية.");
        Add("EmployerFit.Team.Extend",
            "+4 vragen voor scherper beeld",
            "+4 questions for a sharper picture",
            "+4 pytania dla lepszego obrazu",
            "+4 întrebări pentru imagine mai clară",
            "+4 أسئلة لصورة أوضح");
        Add("EmployerFit.T1",
            "Bij ons mag je veel zelf beslissen hoe je je werk doet.",
            "Here you can decide much yourself how you do your work.",
            "U nas możesz sam decydować, jak wykonujesz pracę.",
            "La noi poți decide mult singur cum îți faci munca.",
            "عندنا يمكنك أن تقرر كثيراً بنفسك كيف تعمل.");
        Add("EmployerFit.T2",
            "We doen het informeel: voornaam, korte lijntjes, weinig protocol.",
            "We keep it informal: first names, short lines, little protocol.",
            "Jest luźno: na ty, krótkie linie, mało protokołu.",
            "Suntem informali: pe prenume, linii scurte.",
            "عملنا informal: الاسم الأول وخطوط قصيرة.");
        Add("EmployerFit.T3",
            "We werken het liefst in een team en overleggen vaak.",
            "We prefer working in a team and consulting often.",
            "Wolimy pracę w zespole i częste konsultacje.",
            "Preferăm munca în echipă și consultări dese.",
            "نفضل العمل في فريق والتشاور.");
        Add("EmployerFit.T4",
            "Taken en tijden wisselen regelmatig — flexibiliteit hoort erbij.",
            "Tasks and times change regularly — flexibility is part of it.",
            "Zadania i czasy się zmieniają — elastyczność jest normą.",
            "Sarcinile și orele se schimbă — flexibilitatea face parte.",
            "المهام والأوقات تتغير — المرونة جزء من العمل.");
        Add("EmployerFit.T5",
            "We proberen graag nieuwe manieren; verbeteren hoort bij de baan.",
            "We like trying new ways; improving is part of the job.",
            "Lubimy nowe sposoby; ulepszanie należy do pracy.",
            "Încercăm metode noi; îmbunătățirea face parte din job.",
            "نحب طرقاً جديدة؛ التحسين جزء من الوظيفة.");
        Add("EmployerFit.T6",
            "De mens staat voor het resultaat: we nemen tijd voor elkaar en voor klanten.",
            "People come before results: we take time for each other and customers.",
            "Człowiek przed wynikiem: mamy czas dla siebie i klientów.",
            "Omul înaintea rezultatului: timp pentru colegi și clienți.",
            "الإنسان قبل النتيجة: وقت لبعضنا وللعملاء.");
        Add("EmployerFit.T7",
            "Presteren en doelen halen worden hier zichtbaar beloond.",
            "Performing and hitting goals is visibly rewarded here.",
            "Osiągnięcia i cele są tu widocznie nagradzane.",
            "Performanța și obiectivele sunt răsplătite vizibil.",
            "الأداء وتحقيق الأهداف يُكافَآن هنا.");
        Add("EmployerFit.T8",
            "Vaste afspraken en duidelijke regels geven ons rust op de werkvloer.",
            "Clear rules and fixed agreements give us calm at work.",
            "Stałe ustalenia i jasne zasady dają spokój w pracy.",
            "Reguli clare și acorduri fixe ne liniștesc la muncă.",
            "قواعد واضحة واتفاقات ثابتة تمنحنا هدوءاً.");
        Add("EmployerFit.T9",
            "Collegialiteit en erbij horen zijn minstens zo belangrijk als de taak zelf.",
            "Colleagueship and belonging matter at least as much as the task.",
            "Współpraca i przynależność są równie ważne jak zadanie.",
            "Colegialitatea contează cel puțin la fel de mult.",
            "الزمالة والانتماء مهمان مثل المهمة.");
        Add("EmployerFit.T10",
            "We willen merken dat ons werk ertoe doet voor anderen of de omgeving.",
            "We want to feel our work matters for others or the environment.",
            "Chcemy czuć, że nasza praca ma znaczenie.",
            "Vrem să simțim că munca noastră contează.",
            "نريد أن نشعر أن عملنا مهم.");
        Add("EmployerFit.Vacancy.Accordion",
            "Match scherper stellen (optioneel, ±1 min)",
            "Sharpen matching (optional, ~1 min)",
            "Doprecyzuj dopasowanie (opcjonalnie)",
            "Clarifică match-ul (opțional)",
            "تحسين المطابقة (اختياري)");
        Add("WorkPref.TeamMode.team", "Vooral in team", "Mostly team", "Głównie zespół", "Mai ales echipă", "فريق أساساً");
        Add("WorkPref.TeamMode.mixed", "Mix", "Mix", "Mix", "Mix", "مزيج");
        Add("WorkPref.TeamMode.solo", "Vooral zelfstandig", "Mostly solo", "Głównie solo", "Mai ales solo", "مستقل");
        Add("WorkPref.Structure.fixed", "Vaste procedures", "Fixed procedures", "Stałe procedury", "Proceduri fixe", "إجراءات ثابتة");
        Add("WorkPref.Structure.mixed", "Mix", "Mix", "Mix", "Mix", "مزيج");
        Add("WorkPref.Structure.free", "Veel vrijheid", "Lots of freedom", "Dużo swobody", "Multă libertate", "حرية كبيرة");
        Add("WorkPref.Customer.much", "Veel klantcontact", "Lots of customer contact", "Dużo kontaktu z klientem", "Mult contact clienți", "كثير من contact");
        Add("WorkPref.Customer.some", "Af en toe", "Sometimes", "Czasami", "Uneori", "أحياناً");
        Add("WorkPref.Customer.none", "Weinig/geen", "Little/none", "Mało/brak", "Puțin/deloc", "قليل/لا");
        Add("WorkPref.Shifts.day", "Overdag", "Daytime", "W dzień", "Ziua", "نهاراً");
        Add("WorkPref.Shifts.evening", "Avond", "Evening", "Wieczór", "Seară", "مساءً");
        Add("WorkPref.Shifts.night", "Nacht", "Night", "Noc", "Noapte", "ليلاً");
        Add("WorkPref.Shifts.flex", "Wisselend", "Variable", "Zmienne", "Variabil", "متغير");
        Add("WorkPref.Pace.piek", "Vaak druk", "Often busy", "Często intensywnie", "Des busy", "غالباً مزدحم");
        Add("WorkPref.VacancyPace.calm", "Rustig", "Calm", "Spokojnie", "Calm", "هادئ");
        Add("WorkPref.VacancyPace.norm-ok", "Normaal", "Normal", "Normalnie", "Normal", "عادي");
        Add("WorkPref.VacancyPace.piek", "Vaak piek/druk", "Often peak/busy", "Często szczyt", "Des vârf", "ذروة");
        Add("WorkPref.Place.prefer-indoor", "Binnen", "Indoors", "Wewnątrz", "Interior", "داخل");
        Add("WorkPref.Place.mix", "Mix", "Mix", "Mix", "Mix", "مزيج");
        Add("WorkPref.Place.prefer-outdoor", "Buiten", "Outdoors", "Na zewnątrz", "Exterior", "خارج");
    }
}
