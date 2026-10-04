namespace Jobsy.Web.Localization;

/// <summary>Candidate run-8 copy. Merged last so these strings win in all five locales.</summary>
internal static class UiStringsRun8
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
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

        Add("Kompas.BandSuper",
            "Past heel goed (meer dan 95%)",
            "Fits very well (over 95%)",
            "Pasuje bardzo dobrze (ponad 95%)",
            "Se potrivește foarte bine (peste 95%)",
            "يناسبك جدًا (أكثر من 95%)");
        Add("Kompas.BandStrong",
            "Past goed (meer dan 85%)",
            "Fits well (over 85%)",
            "Pasuje dobrze (ponad 85%)",
            "Se potrivește bine (peste 85%)",
            "يناسبك جيدًا (أكثر من 85%)");
        Add("Career.Holland",
            "Drie letters die laten zien welk soort werk je leuk vindt: {0}",
            "Three letters that show what kind of work you like: {0}",
            "Trzy litery, które pokazują, jaką pracę lubisz: {0}",
            "Trei litere care arată ce fel de muncă îți place: {0}",
            "ثلاثة أحرف تُظهر نوع العمل الذي تحبه: {0}");
        Add("TestResult.Card.Holland.Sub",
            "Drie letters die laten zien welk soort werk je leuk vindt",
            "Three letters that show what kind of work you like",
            "Trzy litery, które pokazują, jaką pracę lubisz",
            "Trei litere care arată ce fel de muncă îți place",
            "ثلاثة أحرف تُظهر نوع العمل الذي تحبه");
        Add("TestResult.Card.Radar",
            "Jij vergeleken met anderen",
            "You compared with others",
            "Ty w porównaniu z innymi",
            "Tu, comparat cu alții",
            "أنت مقارنةً بالآخرين");
        Add("Deep.ValuesTitle",
            "Waarden en drijfveren",
            "Values and drivers",
            "Wartości i motywacje",
            "Valori și motive",
            "القيم والدوافع");

        Add("HowC.Title",
            "In {0} stappen",
            "In {0} steps",
            "W {0} krokach",
            "În {0} pași",
            "في {0} خطوات");
        Add("HowC.Count.1", "1", "1", "1", "1", "1");
        Add("HowC.Count.2", "2", "2", "2", "2", "2");
        Add("HowC.Count.3", "3", "3", "3", "3", "3");
        Add("HowC.Count.4", "4", "4", "4", "4", "4");
        Add("HowC.Count.5", "5", "5", "5", "5", "5");

        Add("Tests.ValuesLead.Autonomy",
            "Je wilt zelf kiezen hoe je je werk doet.",
            "You want to choose how you do your work.",
            "Chcesz sam decydować, jak pracujesz.",
            "Vrei să alegi singur cum îți faci munca.",
            "تريد أن تختار بنفسك كيف تعمل.");
        Add("Tests.ValuesLead.Connection",
            "Je wilt goed contact met de mensen om je heen.",
            "You want good contact with the people around you.",
            "Chcesz dobry kontakt z ludźmi wokół ciebie.",
            "Vrei contact bun cu oamenii din jurul tău.",
            "تريد تواصلًا جيدًا مع الناس من حولك.");
        Add("Tests.ValuesLead.Achievement",
            "Je wilt zien dat je werk iets oplevert.",
            "You want to see that your work leads to something.",
            "Chcesz widzieć, że twoja praca coś daje.",
            "Vrei să vezi că munca ta duce la ceva.",
            "تريد أن ترى أن عملك يؤدي إلى شيء.");
        Add("Tests.ValuesLead.Stability",
            "Je wilt werk dat rustig en duidelijk is.",
            "You want work that is calm and clear.",
            "Chcesz pracy, która jest spokojna i jasna.",
            "Vrei o muncă liniștită și clară.",
            "تريد عملًا هادئًا وواضحًا.");
        Add("Tests.ValuesLead.Impact",
            "Je wilt dat je werk iets doet voor anderen.",
            "You want your work to do something for other people.",
            "Chcesz, żeby twoja praca coś robiła dla innych.",
            "Vrei ca munca ta să facă ceva pentru alții.",
            "تريد أن يفيد عملك الآخرين.");

        Add("Tests.CultureLead.Autonomy",
            "Je werkt het liefst zelfstandig.",
            "You prefer to work on your own.",
            "Najchętniej pracujesz samodzielnie.",
            "Preferi să lucrezi pe cont propriu.",
            "تفضل أن تعمل بمفردك.");
        Add("Tests.CultureLead.Informal",
            "Je houdt van een losse sfeer.",
            "You like a relaxed atmosphere.",
            "Lubisz swobodną atmosferę.",
            "Îți place o atmosferă relaxată.",
            "تحب جوًا مريحًا.");
        Add("Tests.CultureLead.Collaboration",
            "Je werkt graag samen met anderen.",
            "You like working together with others.",
            "Lubisz pracować razem z innymi.",
            "Îți place să lucrezi împreună cu alții.",
            "تحب العمل مع الآخرين.");
        Add("Tests.CultureLead.Flexibility",
            "Je past je makkelijk aan.",
            "You adapt easily.",
            "Łatwo się dostosowujesz.",
            "Te adaptezi ușor.",
            "تتكيف بسهولة.");
        Add("Tests.CultureLead.Innovation",
            "Je zoekt graag een nieuwe manier.",
            "You like looking for a new way.",
            "Lubisz szukać nowego sposobu.",
            "Îți place să cauți un mod nou.",
            "تحب البحث عن طريقة جديدة.");
        Add("Tests.CultureLead.PeopleFirst",
            "Mensen komen bij jou op de eerste plek.",
            "People come first for you.",
            "Ludzie są u ciebie na pierwszym miejscu.",
            "Oamenii sunt pe primul loc pentru tine.",
            "الناس يأتون عندك في المقام الأول.");
    }
}
