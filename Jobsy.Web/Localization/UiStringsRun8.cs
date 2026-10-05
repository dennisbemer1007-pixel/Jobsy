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
            "Past het best",
            "Best match",
            "Pasuje najlepiej",
            "Se potrivește cel mai bine",
            "الأنسب");
        Add("Kompas.BandStrong",
            "Past goed",
            "Good match",
            "Pasuje dobrze",
            "Se potrivește bine",
            "يناسبك جيدًا");
        Add("Kompas.BandBroaden",
            "Ook de moeite",
            "Also worth a look",
            "Też warto",
            "Merită și asta",
            "يستحق النظر أيضًا");
        Add("Kompas.FitExplainer",
            "Het percentage is hoe goed je hele profiel bij dit beroep past. De plek in de lijst telt mee.",
            "The percentage is how well your whole profile fits this job. The place in the list matters too.",
            "Procent to dopasowanie całego profilu do tego zawodu. Miejsce na liście też się liczy.",
            "Procentul arată cât de bine se potrivește tot profilul tău cu această meserie. Locul în listă contează și el.",
            "النسبة هي مدى تناسب ملفك كله مع هذه المهنة. الترتيب في القائمة مهم أيضًا.");
        Add("Kompas.RankChoice",
            "{0}e keus",
            "choice {0}",
            "{0}. wybór",
            "alegerea {0}",
            "الخيار {0}");
        Add("Career.ClearDream",
            "Droombaan wissen",
            "Clear dream job",
            "Usuń wymarzoną pracę",
            "Șterge jobul de vis",
            "امسح وظيفة الأحلام");
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
            "Uit je test blijkt dat zelfstandig werken bij je past.",
            "Your test shows that working on your own fits you.",
            "Z twojego testu wynika, że samodzielna praca do ciebie pasuje.",
            "Din testul tău reiese că munca pe cont propriu ți se potrivește.",
            "يظهر من اختبارك أن العمل بمفردك يناسبك.");
        Add("Tests.CultureLead.Informal",
            "Uit je test blijkt dat een losse sfeer bij je past.",
            "Your test shows that a relaxed atmosphere fits you.",
            "Z twojego testu wynika, że swobodna atmosfera do ciebie pasuje.",
            "Din testul tău reiese că o atmosferă relaxată ți se potrivește.",
            "يظهر من اختبارك أن الجو المريح يناسبك.");
        Add("Tests.CultureLead.Collaboration",
            "Uit je test blijkt dat samenwerken bij je past.",
            "Your test shows that working together fits you.",
            "Z twojego testu wynika, że wspólna praca do ciebie pasuje.",
            "Din testul tău reiese că lucrul împreună ți se potrivește.",
            "يظهر من اختبارك أن العمل مع الآخرين يناسبك.");
        Add("Tests.CultureLead.Flexibility",
            "Uit je test blijkt dat flexibel meebewegen bij je past.",
            "Your test shows that moving with change fits you.",
            "Z twojego testu wynika, że dopasowanie się do ciebie pasuje.",
            "Din testul tău reiese că adaptarea ți se potrivește.",
            "يظهر من اختبارك أن المرونة تناسبك.");
        Add("Tests.CultureLead.Innovation",
            "Uit je test blijkt dat nieuwe manieren bij je passen.",
            "Your test shows that a new way fits you.",
            "Z twojego testu wynika, że nowy sposób do ciebie pasuje.",
            "Din testul tău reiese că un mod nou ți se potrivește.",
            "يظهر من اختبارك أن الطريقة الجديدة تناسبك.");
        Add("Tests.CultureLead.PeopleFirst",
            "Uit je test blijkt dat mensen voorop bij je past.",
            "Your test shows that people first fits you.",
            "Z twojego testu wynika, że ludzie na pierwszym miejscu do ciebie pasują.",
            "Din testul tău reiese că oamenii pe primul loc ți se potrivesc.",
            "يظهر من اختبارك أن الناس أولاً يناسبك.");
    }
}
