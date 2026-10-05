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
            "Past heel goed (95% of meer)",
            "Fits very well (95% or more)",
            "Pasuje bardzo dobrze (95% lub więcej)",
            "Se potrivește foarte bine (95% sau mai mult)",
            "يناسبك جدًا (95% أو أكثر)");
        Add("Kompas.BandStrong",
            "Past goed (85% tot 94%)",
            "Fits well (85% to 94%)",
            "Pasuje dobrze (85% do 94%)",
            "Se potrivește bine (85% până la 94%)",
            "يناسبك جيدًا (من 85% إلى 94%)");
        Add("Kompas.BandBroaden",
            "Ook de moeite (75% tot 84%)",
            "Also worth a look (75% to 84%)",
            "Też warto (75% do 84%)",
            "Merită și asta (75% până la 84%)",
            "يستحق النظر أيضًا (من 75% إلى 84%)");
        Add("Kompas.BandRankTop",
            "Past het best bij je scores",
            "Best match for your scores",
            "Najlepiej pasuje do twoich wyników",
            "Se potrivește cel mai bine cu scorurile tale",
            "الأنسب لنتائجك");
        Add("Kompas.BandRankMid",
            "Past ook goed bij je scores",
            "Also a good match for your scores",
            "Też dobrze pasuje do twoich wyników",
            "Se potrivește bine și cu scorurile tale",
            "يناسب نتائجك أيضاً");
        Add("Kompas.BandRankAlso",
            "Ook de moeite waard",
            "Also worth a look",
            "Też warto zobaczyć",
            "Merită și asta",
            "يستحق النظر أيضاً");
        Add("Assistant.NewChat",
            "Nieuw gesprek",
            "New chat",
            "Nowa rozmowa",
            "Conversație nouă",
            "محادثة جديدة");
        Add("Kompas.TabDna",
            "Wie ik ben",
            "Who I am",
            "Kim jestem",
            "Cine sunt",
            "من أنا");
        Add("TestResult.Upsell.Steps",
            "Concrete stappen voor je loopbaanplan",
            "Concrete steps for your career plan",
            "Konkretne kroki do twojego planu kariery",
            "Pași concreți pentru planul tău",
            "خطوات واضحة لخطة مسارك");
        Add("TestResult.Card.Holland",
            "Jouw beroepsletters",
            "Your job letters",
            "Twoje litery zawodowe",
            "Literele tale de meserie",
            "حروف مهنتك");
        Add("HowLobsy.SignedIn.PassportTitle",
            "Ga naar je paspoort",
            "Go to your passport",
            "Przejdź do paszportu",
            "Mergi la pașaport",
            "اذهب إلى جوازك");
        Add("HowLobsy.SignedIn.PassportBody",
            "Je tests staan al bij je paspoort. Je hoeft geen nieuw account te maken.",
            "Your tests are already on your passport. You do not need a new account.",
            "Twoje testy są już w paszporcie. Nie musisz zakładać nowego konta.",
            "Testele tale sunt deja în pașaport. Nu ai nevoie de un cont nou.",
            "اختباراتك موجودة في جوازك. لا تحتاج إلى حساب جديد.");
        Add("HowLobsy.SignedIn.PassportCta",
            "Naar mijn paspoort",
            "Go to my passport",
            "Do mojego paszportu",
            "La pașaportul meu",
            "إلى جوازي");
        Add("HowLobsy.Title.Counted",
            "Zo werkt Lobsy. In {0} stappen.",
            "How Lobsy works. In {0} steps.",
            "Jak działa Lobsy. W {0} krokach.",
            "Cum funcționează Lobsy. În {0} pași.",
            "كيف يعمل Lobsy. في {0} خطوات.");
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
