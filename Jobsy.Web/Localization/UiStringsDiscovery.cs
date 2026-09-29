namespace Jobsy.Web.Localization;

public static class UiStringsDiscovery
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

        // Dutch levels
        Add("Discovery.Dutch.beginner",
            "Beginner", "Beginner level", "Początkujący", "Începător", "مبتدئ");
        Add("Discovery.Dutch.beginner.Explain",
            "Ik leer het net (A1–A2).", "I’m just starting (A1–A2).", "Dopiero się uczę (A1–A2).", "Abia învăț (A1–A2).", "أتعلم للتو (A1–A2).");
        Add("Discovery.Dutch.basis",
            "Basis", "Basic", "Podstawowy", "De bază", "أساسي");
        Add("Discovery.Dutch.basis.Explain",
            "Ik red me in gewone gesprekken (B1).", "I manage in everyday talks (B1).", "Radzę sobie w zwykłych rozmowach (B1).", "Mă descurc în conversații obișnuite (B1).", "أتعامل في المحادثات العادية (B1).");
        Add("Discovery.Dutch.goed",
            "Goed", "Good", "Dobry", "Bun", "جيد");
        Add("Discovery.Dutch.goed.Explain",
            "Ik praat vlot over werk en privé (B2).", "I speak fluently about work and life (B2).", "Swobodnie mówię o pracy i życiu (B2).", "Vorbesc fluent despre muncă și viață (B2).", "أتحدث بطلاقة عن العمل والحياة (B2).");
        Add("Discovery.Dutch.vloeiend",
            "Vloeiend", "Fluent", "Płynny", "Fluent", "بطلاقة");
        Add("Discovery.Dutch.vloeiend.Explain",
            "Bijna als een moedertaal (C1–C2).", "Almost like a native speaker (C1–C2).", "Prawie jak język ojczysty (C1–C2).", "Aproape ca limba maternă (C1–C2).", "تقريباً كلغة أم (C1–C2).");
        Add("Discovery.Dutch.moedertaal",
            "Moedertaal", "Native", "Ojczysty", "Limbă maternă", "لغة أم");
        Add("Discovery.Dutch.moedertaal.Explain",
            "Nederlands is mijn moedertaal.", "Dutch is my native language.", "Niderlandzki to mój język ojczysty.", "Neerlandezas este limba mea maternă.", "الهولندية لغتي الأم.");

        // Employer preferences
        Add("Discovery.Employer.small-team",
            "Klein, warm team", "Small, warm team", "Mały, ciepły zespół", "Echipă mică și caldă", "فريق صغير ودافئ");
        Add("Discovery.Employer.large-company",
            "Groot bedrijf", "Large company", "Duża firma", "Companie mare", "شركة كبيرة");
        Add("Discovery.Employer.fixed-workplace",
            "Vaste werkplek", "Fixed workplace", "Stałe miejsce pracy", "Loc de muncă fix", "مكان عمل ثابت");
        Add("Discovery.Employer.learn-on-the-job",
            "Leren op de werkvloer", "Learn on the job", "Nauka w pracy", "Învățare la locul de muncă", "التعلم في العمل");
        Add("Discovery.Employer.dutch-support",
            "Hulp met Nederlands", "Help with Dutch", "Pomoc z niderlandzkim", "Ajutor cu neerlandeză", "مساعدة في الهولندية");
        Add("Discovery.Employer.growth",
            "Kans om door te groeien", "Chance to grow", "Szansa na rozwój", "Șansă de creștere", "فرصة للتطور");
        Add("Discovery.Employer.close-to-home",
            "Dichtbij huis", "Close to home", "Blisko domu", "Aproape de casă", "قريب من المنزل");
        Add("Discovery.Employer.variety",
            "Veel afwisseling", "Lots of variety", "Dużo różnorodności", "Multă varietate", "تنوع كبير");

        // Hobbies
        Add("Discovery.Hobby.sport", "Sport", "Sports", "Sporty", "Sporturi", "رياضة");
        Add("Discovery.Hobby.music", "Muziek", "Music", "Muzyka", "Muzică", "موسيقى");
        Add("Discovery.Hobby.cooking", "Koken", "Cooking", "Gotowanie", "Gătit", "طبخ");
        Add("Discovery.Hobby.gaming", "Games", "Gaming", "Gry", "Jocuri", "ألعاب");
        Add("Discovery.Hobby.crafts", "Knutselen", "Crafts", "Rękodzieło", "Meșteșuguri", "حِرَف يدوية");
        Add("Discovery.Hobby.nature", "Natuur", "Nature", "Natura", "Natură", "طبيعة");
        Add("Discovery.Hobby.reading", "Lezen", "Reading", "Czytanie", "Citit", "قراءة");
        Add("Discovery.Hobby.caring", "Zorgen voor anderen", "Caring", "Opieka", "Grijă de alții", "رعاية الآخرين");
        Add("Discovery.Hobby.tech", "Techniek", "Tech", "Technika", "Tehnică", "تقنية");
        Add("Discovery.Hobby.art", "Kunst", "Art", "Sztuka", "Artă", "فن");
        Add("Discovery.Hobby.volunteering", "Vrijwilligerswerk", "Volunteering", "Wolontariat", "Voluntariat", "تطوع");
        Add("Discovery.Hobby.fashion", "Mode", "Fashion", "Moda", "Modă", "أزياء");

        // Dislikes
        Add("Discovery.Dislike.night-shifts",
            "Nachtdiensten", "Night shifts", "Nocne zmiany", "Ture de noapte", "ورديات ليلية");
        Add("Discovery.Dislike.heavy-lifting",
            "Zwaar tillen", "Heavy lifting", "Ciężkie podnoszenie", "Ridicat greu", "رفع ثقيل");
        Add("Discovery.Dislike.working-alone",
            "Veel alleen werken", "Working alone a lot", "Dużo pracy w samotności", "Multă muncă singur", "العمل وحدك كثيراً");
        Add("Discovery.Dislike.phone-customers",
            "Bellen met klanten", "Calling customers", "Rozmowy z klientami", "Apeluri cu clienții", "الاتصال بالعملاء");
        Add("Discovery.Dislike.noise",
            "Lawaai", "Noise", "Hałas", "Zgomot", "ضوضاء");
        Add("Discovery.Dislike.cold-outdoor",
            "Kou of veel buiten", "Cold or outdoors a lot", "Zimno lub dużo na zewnątrz", "Frig sau mult afară", "برد أو عمل خارجي كثير");
        Add("Discovery.Dislike.computer-work",
            "Veel computerwerk", "Lots of computer work", "Dużo pracy przy komputerze", "Multă muncă la calculator", "عمل حاسوب كثير");
        Add("Discovery.Dislike.changing-hours",
            "Steeds andere tijden", "Always changing hours", "Ciągle inne godziny", "Ore mereu schimbătoare", "أوقات متغيرة دائماً");
        Add("Discovery.Dislike.crowded",
            "Drukke plekken", "Busy places", "Tłoczne miejsca", "Locuri aglomerate", "أماكن مزدحمة");
        Add("Discovery.Dislike.long-travel",
            "Lang reizen", "Long travel", "Długi dojazd", "Deplasare lungă", "تنقل طويل");
        Add("Discovery.Dislike.PrivateNote",
            "Alleen voor jou en je matches. Werkgevers zien dit niet.",
            "Only for you and your matches. Employers do not see this.",
            "Tylko dla Ciebie i Twoich dopasowań. Pracodawcy tego nie widzą.",
            "Doar pentru tine și potrivirile tale. Angajatorii nu văd asta.",
            "لك ولمطابقاتك فقط. أصحاب العمل لا يرون هذا.");
        Add("Discovery.Dislike.SomethingElse",
            "Iets anders", "Something else", "Coś innego", "Altceva", "شيء آخر");
        Add("Discovery.Dislike.CustomPlaceholder",
            "Wat liever niet?", "What would you rather avoid?", "Czego wolisz unikać?", "Ce ai prefera să eviți?", "ماذا تفضّل تجنّبه؟");

        // Passport display
        Add("Passport.Facts.Languages",
            "Talen", "Languages", "Języki", "Limbi", "اللغات");
        Add("Passport.Dna.Joy",
            "Waar word je blij van", "What makes you happy", "Co Cię cieszy", "Ce te bucură", "ما يسعدك");
        Add("Passport.Proof.WantToLearn",
            "Wat ik nog wil leren", "What I still want to learn", "Czego jeszcze chcę się nauczyć", "Ce vreau să mai învăț", "ما أريد تعلمه بعد");

        // Profile sections
        Add("Profile.Section.JoyAndLearning",
            "Wat ik leuk vind en wil leren", "What I like and want to learn", "Co lubię i czego chcę się uczyć", "Ce-mi place și ce vreau să învăț", "ما أحبه وما أريد تعلمه");
        Add("Profile.Section.Dislikes",
            "Waar houd ik niet van", "What I don’t like", "Czego nie lubię", "Ce nu-mi place", "ما لا أحبه");
        Add("Profile.SpokenLanguages",
            "Talen", "Languages", "Języki", "Limbi", "اللغات");
        Add("Profile.DutchLevel",
            "Niveau Nederlands", "Dutch level", "Poziom niderlandzkiego", "Nivel de neerlandeză", "مستوى الهولندية");
        Add("Profile.EmployerPreferences",
            "Wat voor werkgever zoek ik?", "What kind of employer am I looking for?", "Jakiego pracodawcy szukam?", "Ce fel de angajator caut?", "أي نوع من أصحاب العمل أبحث عنه؟");
        Add("Profile.Hobbies",
            "Hobby’s", "Hobbies", "Hobby", "Hobby-uri", "هوايات");
        Add("Profile.LearningGoals",
            "Wat ik nog wil leren", "What I still want to learn", "Czego jeszcze chcę się nauczyć", "Ce vreau să mai învăț", "ما أريد تعلمه بعد");
        Add("Profile.LearningGoalPlaceholder",
            "Bijv. beter plannen", "E.g. better planning", "Np. lepsze planowanie", "Ex. planificare mai bună", "مثلاً تخطيط أفضل");
        Add("Profile.AddLanguage",
            "Taal toevoegen", "Add language", "Dodaj język", "Adaugă limbă", "إضافة لغة");
        Add("Profile.LanguageLevel",
            "Niveau", "Level", "Poziom", "Nivel", "المستوى");
        Add("Passport.Data.JoySub",
            "Hobby’s en leerdoelen", "Hobbies and learning goals", "Hobby i cele nauki", "Hobby-uri și obiective de învățare", "هوايات وأهداف تعلم");
        Add("Passport.Data.DislikesSub",
            "Privé — alleen voor matches", "Private — matches only", "Prywatne — tylko dopasowania", "Privat — doar potriviri", "خاص — للمطابقات فقط");
    }
}
