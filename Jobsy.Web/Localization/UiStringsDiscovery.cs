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

        // Journey shell (07a)
        Add("Discovery.PageTitle",
            "De ontdekkingsreis", "The discovery journey", "Podróż odkrywcza", "Călătoria descoperirii", "رحلة الاكتشاف");
        Add("Discovery.Eyebrow",
            "De ontdekkingsreis", "The discovery journey", "Podróż odkrywcza", "Călătoria descoperirii", "رحلة الاكتشاف");
        Add("Discovery.Lobster.Aria",
            "Lobsy de kreeft", "Lobsy the lobster", "Lobsy homar", "Lobsy homarul", "لوبسي الكركند");
        Add("Discovery.Later",
            "Later verder", "Continue later", "Później dalej", "Mai târziu", "المتابعة لاحقاً");
        Add("Discovery.Zone.Beach",
            "Het strand", "The beach", "Plaża", "Plaja", "الشاطئ");
        Add("Discovery.Zone.Coast",
            "Aan de kust", "At the coast", "Przy brzegu", "La coastă", "على الساحل");
        Add("Discovery.Zone.Rocks",
            "Tussen de rotsen", "Between the rocks", "Między skałami", "Între stânci", "بين الصخور");
        Add("Discovery.Zone.Deep",
            "In de diepte", "In the deep", "W głębinie", "În adânc", "في الأعماق");
        Add("Discovery.Zone.Light",
            "Naar het licht", "Toward the light", "Ku światłu", "Spre lumină", "نحو الضوء");
        Add("Discovery.Rail.Title",
            "Jouw reis", "Your journey", "Twoja podróż", "Călătoria ta", "رحلتك");
        Add("Discovery.Rail.Aria",
            "Jouw reis, van het strand naar de diepte", "Your journey, from the beach to the deep", "Twoja podróż, od plaży do głębin", "Călătoria ta, de la plajă spre adânc", "رحلتك من الشاطئ إلى الأعماق");
        Add("Discovery.Rail.Start",
            "Start", "Starting point", "Punkt startowy", "Punct de start", "البداية");
        Add("Discovery.Rail.NewShell",
            "Je nieuwe schaal", "Your new shell", "Twoja nowa skorupa", "Noua ta carapace", "صدفتك الجديدة");
        Add("Discovery.Rail.LayerOff",
            "Laag {0} eraf", "Layer {0} off", "Warstwa {0} zdjęta", "Stratul {0} jos", "الطبقة {0} انتهت");
        Add("Discovery.Rail.LayersAria",
            "Oude schaal: {0} van 10 lagen eraf", "Old shell: {0} of 10 layers off", "Stara skorupa: {0} z 10 warstw", "Carapacea veche: {0} din 10 straturi", "الصدفة القديمة: {0} من 10 طبقات");
        Add("Discovery.Rail.LayersCount",
            "{0} van 10 lagen eraf", "{0} of 10 layers off", "{0} z 10 warstw", "{0} din 10 straturi", "{0} من 10 طبقات");
        Add("Discovery.Rail.StartCount",
            "10 stappen · ± 12 minuten", "10 steps · ± 12 minutes", "10 kroków · ± 12 minut", "10 pași · ± 12 minute", "10 خطوات · ± 12 دقيقة");
        Add("Discovery.Rail.StartCountShort",
            "10 stappen · ± 12 min", "10 steps · ± 12 min", "10 kroków · ± 12 min", "10 pași · ± 12 min", "10 خطوات · ± 12 د");
        Add("Discovery.Rail.StepCount",
            "Stap {0} van 10", "Step {0} of 10", "Krok {0} z 10", "Pasul {0} din 10", "الخطوة {0} من 10");
        Add("Discovery.Rail.DoneCount",
            "Klaar · 10 van 10 · nieuwe schaal", "Done · 10 of 10 · new shell", "Gotowe · 10 z 10 · nowa skorupa", "Gata · 10 din 10 · carapace nouă", "تم · 10 من 10 · صدفة جديدة");
        Add("Discovery.StepOf",
            "stap {0} van 10", "step {0} of 10", "krok {0} z 10", "pasul {0} din 10", "الخطوة {0} من 10");
        Add("Discovery.Saved.Ok",
            "Alles is bewaard. Stoppen mag altijd.", "Everything is saved. You can always stop.", "Wszystko zapisane. Zawsze możesz przerwać.", "Totul e salvat. Poți opri oricând.", "تم الحفظ. يمكنك التوقف دائماً.");
        Add("Discovery.Saved.Saving",
            "Bewaren…", "Saving…", "Zapisywanie…", "Se salvează…", "جارٍ الحفظ…");
        Add("Discovery.Saved.Error",
            "Niet bewaard. We proberen het opnieuw.", "Not saved. We’ll try again.", "Nie zapisano. Spróbujemy ponownie.", "Nesalvat. Încercăm din nou.", "لم يُحفظ. نحاول مجدداً.");
        Add("Discovery.Saved.Short",
            "Bewaard", "Saved", "Zapisano", "Salvat", "محفوظ");
        Add("Discovery.Start.Title",
            "Hoi {0}, fijn dat je er bent", "Hi {0}, glad you’re here", "Cześć {0}, miło że jesteś", "Salut {0}, ne bucurăm că ești aici", "مرحباً {0}، سعداء بوجودك");
        Add("Discovery.Start.Lead",
            "Samen ontdekken we wie je bent en welk werk bij je past. Laag voor laag. Aan het eind staat alles in je paspoort.",
            "Together we discover who you are and what work fits you. Layer by layer. At the end it’s all in your passport.",
            "Razem odkryjemy kim jesteś i jaka praca do Ciebie pasuje. Warstwa po warstwie. Na końcu wszystko będzie w paszporcie.",
            "Împreună descoperim cine ești și ce muncă ți se potrivește. Strat cu strat. La final totul e în pașaport.",
            "معاً نكتشف من أنت وأي عمل يناسبك. طبقة تلو الأخرى. في النهاية كل شيء في جوازك.");
        Add("Discovery.Start.LeadEmployersOff",
            "Samen ontdekken we wie je bent. Laag voor laag. Aan het eind staat alles in je paspoort.",
            "Together we discover who you are. Layer by layer. At the end it’s all in your passport.",
            "Razem odkryjemy kim jesteś. Warstwa po warstwie. Na końcu wszystko będzie w paszporcie.",
            "Împreună descoperim cine ești. Strat cu strat. La final totul e în pașaport.",
            "معاً نكتشف من أنت. طبقة تلو الأخرى. في النهاية كل شيء في جوازك.");
        Add("Discovery.Start.CoastLead",
            "Over jou, en wanneer je kunt werken", "About you, and when you can work", "O Tobie i kiedy możesz pracować", "Despre tine și când poți lucra", "عنك ومتى يمكنك العمل");
        Add("Discovery.Start.CoastTime", "2 min", "2 min", "2 min", "2 min", "٢ د");
        Add("Discovery.Start.RocksLead",
            "Je werk, wat je leerde en wat je leuk vindt", "Your work, what you learned and what you like", "Twoja praca, nauka i to co lubisz", "Munca ta, ce ai învățat și ce-ți place", "عملك وما تعلمته وما تحب");
        Add("Discovery.Start.RocksTime", "4 min", "4 min", "4 min", "4 min", "٤ د");
        Add("Discovery.Start.DeepLead",
            "4 korte tests van 5 vragen", "4 short tests of 5 questions", "4 krótkie testy po 5 pytań", "4 teste scurte de 5 întrebări", "٤ اختبارات قصيرة من ٥ أسئلة");
        Add("Discovery.Start.DeepTime", "5 min", "5 min", "5 min", "5 min", "٥ د");
        Add("Discovery.Start.HintAnswers",
            "Er zijn geen foute antwoorden. Het maakt niet uit waar je nu staat.",
            "There are no wrong answers. It doesn’t matter where you are now.",
            "Nie ma złych odpowiedzi. Nie ma znaczenia, gdzie teraz jesteś.",
            "Nu există răspunsuri greșite. Nu contează unde ești acum.",
            "لا توجد إجابات خاطئة. لا يهم أين أنت الآن.");
        Add("Discovery.Start.HintSaved",
            "Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was.",
            "Everything is saved immediately. You can stop and continue later where you left off.",
            "Wszystko zapisuje się od razu. Możesz przerwać i wrócić później.",
            "Totul se salvează imediat. Poți opri și continua mai târziu.",
            "يُحفظ كل شيء فوراً. يمكنك التوقف والمتابعة لاحقاً.");
        Add("Discovery.Start.HintLanguage",
            "Liever een andere taal? Kies je taal rechtsboven.",
            "Prefer another language? Choose it at the top right.",
            "Inny język? Wybierz go u góry po prawej.",
            "Altă limbă? Alege-o sus în dreapta.",
            "لغة أخرى؟ اخترها أعلى اليمين.");
        Add("Discovery.Start.Cta",
            "Begin de reis", "Start the journey", "Zacznij podróż", "Începe călătoria", "ابدأ الرحلة");
        Add("Discovery.Step1.Title", "Over jou", "About you", "O Tobie", "Despre tine", "عنك");
        Add("Discovery.Step1.Sub", "Persoonlijke gegevens", "Personal details", "Dane osobowe", "Date personale", "بيانات شخصية");
        Add("Discovery.Step1.PostcodeResolved",
            "{0} · alleen voor reistijd, werkgevers zien je adres niet",
            "{0} · travel time only, employers don’t see your address",
            "{0} · tylko czas dojazdu, pracodawcy nie widzą adresu",
            "{0} · doar pentru timpul de drum, angajatorii nu văd adresa",
            "{0} · فقط لمدة السفر، أصحاب العمل لا يرون عنوانك");
        Add("Discovery.Step1.BirthHintEmployersOff",
            "We gebruiken je leeftijd alleen om te kijken of je toestemming van een ouder nodig hebt.",
            "We only use your age to check if you need a parent’s consent.",
            "Wiek używamy tylko, by sprawdzić czy potrzebujesz zgody rodzica.",
            "Folosim vârsta doar ca să vedem dacă ai nevoie de acordul unui părinte.",
            "نستخدم عمرك فقط لمعرفة إن كنت تحتاج موافقة ولي الأمر.");
        Add("Discovery.Step2.Title", "Wanneer en hoe", "When and how", "Kiedy i jak", "Când și cum", "متى وكيف");
        Add("Discovery.Step2.Sub", "Beschikbaarheid en reizen", "Availability and travel", "Dostępność i dojazd", "Disponibilitate și deplasare", "التوفر والتنقل");
        Add("Discovery.Step3.Title", "Werk", "Work", "Praca", "Muncă", "العمل");
        Add("Discovery.Step3.Sub", "Waar ik werkte en wat ik zoek", "Where I worked and what I seek", "Gdzie pracowałem i czego szukam", "Unde am lucrat și ce caut", "أين عملت وما أبحث عنه");
        Add("Discovery.Step3.Heading",
            "Jouw werk", "Your work", "Twoja praca", "Munca ta", "عملك");
        Add("Discovery.Step3.Lead",
            "Waar heb je gewerkt, en wat voor werkgever zoek je?",
            "Where have you worked, and what kind of employer are you looking for?",
            "Gdzie pracowałeś i jakiego pracodawcy szukasz?",
            "Unde ai lucrat și ce fel de angajator cauți?",
            "أين عملت، وأي نوع من أصحاب العمل تبحث عنه؟");
        Add("Discovery.Step3.Worked",
            "Waar heb ik gewerkt?", "Where have I worked?", "Gdzie pracowałem?", "Unde am lucrat?", "أين عملت؟");
        Add("Discovery.Step3.WorkedHint",
            "Bijbaan, vrijwilligerswerk, stage of zorgen voor familie telt ook mee.",
            "Side jobs, volunteering, internships or caring for family count too.",
            "Dorywcza praca, wolontariat, staż czy opieka nad rodziną też się liczą.",
            "Jobul ocazional, voluntariatul, stagiul sau grija de familie contează și ele.",
            "العمل الجزئي والتطوع والتدريب ورعاية العائلة تُحسب أيضاً.");
        Add("Discovery.Step3.EmployerWant",
            "Wat voor werkgever zoek ik?", "What kind of employer am I looking for?", "Jakiego pracodawcy szukam?", "Ce fel de angajator caut?", "أي نوع من أصحاب العمل أبحث عنه؟");
        Add("Discovery.Step3.EmployerWantHint",
            "Kies wat voor jou belangrijk is. Meer kiezen mag.",
            "Choose what matters to you. Picking more is fine.",
            "Wybierz, co jest dla Ciebie ważne. Możesz wybrać więcej.",
            "Alege ce contează pentru tine. Poți alege mai multe.",
            "اختر ما يهمك. يمكنك اختيار أكثر من خيار.");
        Add("Discovery.Step3.Field",
            "In welk werkveld wil ik werken?", "Which field do I want to work in?", "W jakiej branży chcę pracować?", "În ce domeniu vreau să lucrez?", "في أي مجال أريد العمل؟");
        Add("Discovery.Step4.Title", "Leren", "Learning", "Nauka", "Învățare", "التعلم");
        Add("Discovery.Step4.Sub", "Wat ik deed en nog wil leren", "What I did and still want to learn", "Czego się uczyłem i chcę się nauczyć", "Ce am făcut și vreau să învăț", "ما فعلته وما أريد تعلمه");
        Add("Discovery.Step4.Heading",
            "Leren", "Learning", "Nauka", "Învățare", "التعلم");
        Add("Discovery.Step4.Lead",
            "Wat heb je geleerd, en wat wil je nog leren?",
            "What have you learned, and what do you still want to learn?",
            "Czego się nauczyłeś i czego jeszcze chcesz się nauczyć?",
            "Ce ai învățat și ce mai vrei să înveți?",
            "ماذا تعلمت، وماذا تريد أن تتعلم بعد؟");
        Add("Discovery.Step4.DreamTitle",
            "Waar wil ik naartoe?", "Where do I want to go?", "Dokąd chcę dojść?", "Unde vreau să ajung?", "إلى أين أريد أن أصل؟");
        Add("Discovery.Step4.LearnWant",
            "Wat wil ik nog leren?", "What do I still want to learn?", "Czego jeszcze chcę się nauczyć?", "Ce mai vreau să învăț?", "ماذا أريد أن أتعلم بعد؟");
        Add("Discovery.Step4.CertificatesSummary",
            "Certificaten", "Certificates", "Certyfikaty", "Certificate", "الشهادات");
        Add("Discovery.Step4.LanguagesSummary",
            "Talen die ik spreek", "Languages I speak", "Języki, które znam", "Limbile pe care le vorbesc", "اللغات التي أتحدثها");
        Add("Discovery.Step5.Title", "Wat ik leuk vind", "What I like", "Co lubię", "Ce-mi place", "ما أحبه");
        Add("Discovery.Step5.Sub", "Interesses en hobby’s", "Interests and hobbies", "Zainteresowania i hobby", "Interese și hobby-uri", "اهتمامات وهوايات");
        Add("Discovery.Step5.Heading",
            "Wat ik leuk vind", "What I like", "Co lubię", "Ce-mi place", "ما أحبه");
        Add("Discovery.Step5.Lead",
            "Hobby’s en wat je blij maakt. Daar zit vaak je kracht.",
            "Hobbies and what makes you happy. That’s often where your strength is.",
            "Hobby i to, co Cię cieszy. Tam często jest Twoja siła.",
            "Hobby-uri și ce te bucură. Acolo e adesea puterea ta.",
            "هوايات وما يسعدك. هناك غالباً تكون قوتك.");
        Add("Discovery.Step5.Hobbies",
            "Hobby’s", "Hobbies", "Hobby", "Hobby-uri", "هوايات");
        Add("Discovery.Step5.HobbiesHint",
            "Kies wat bij je past. Meer mag.",
            "Pick what fits you. More is fine.",
            "Wybierz, co do Ciebie pasuje. Możesz więcej.",
            "Alege ce ți se potrivește. Poți mai multe.",
            "اختر ما يناسبك. يمكنك المزيد.");
        Add("Discovery.Step5.AboutMe",
            "Over mij", "About me", "O mnie", "Despre mine", "عني");
        Add("Discovery.Step5.AboutMeHint",
            "Optioneel — een paar zinnen over jezelf.",
            "Optional — a few sentences about yourself.",
            "Opcjonalnie — kilka zdań o sobie.",
            "Opțional — câteva propoziții despre tine.",
            "اختياري — بضع جمل عن نفسك.");
        Add("Discovery.Step5.AboutMeCount",
            "{0} / 500", "{0} of 500", "{0} z 500", "{0} din 500", "{0} / ٥٠٠");
        Add("Discovery.Step6.Title", "Waar houd ik niet van", "What I don’t like", "Czego nie lubię", "Ce nu-mi place", "ما لا أحبه");
        Add("Discovery.Step6.Sub", "Overslaan mag", "Skipping is fine", "Można pominąć", "Poți sări", "يمكن التخطي");
        Add("Discovery.Step6.Heading",
            "Waar houd ik niet van?", "What don’t I like?", "Czego nie lubię?", "Ce nu-mi place?", "ما لا أحبه؟");
        Add("Discovery.Step6.Lead",
            "Ook dat is goed om te weten. Dan laten we minder werk zien dat niet bij je past.",
            "That’s useful to know too. Then we show less work that doesn’t fit you.",
            "To też warto wiedzieć. Pokażemy mniej pracy, która do Ciebie nie pasuje.",
            "Și asta e bine de știut. Atunci arătăm mai puțină muncă care nu ți se potrivește.",
            "هذا أيضاً مفيد معرفته. عندها نعرض عملاً أقل لا يناسبك.");
        Add("Discovery.Step6.LeadEmployersOff",
            "Ook dat is goed om te weten. Dan weten we beter wat bij je past.",
            "That’s useful to know too. Then we better understand what fits you.",
            "To też warto wiedzieć. Lepiej zrozumiemy, co do Ciebie pasuje.",
            "Și asta e bine de știut. Înțelegem mai bine ce ți se potrivește.",
            "هذا أيضاً مفيد معرفته. عندها نفهم أفضل ما يناسبك.");
        Add("Discovery.Step6.Question",
            "Wat liever niet?", "What would you rather avoid?", "Czego wolisz unikać?", "Ce ai prefera să eviți?", "ماذا تفضّل تجنّبه؟");
        Add("Discovery.Step6.QuestionHint",
            "Kies wat je wilt. Niets kiezen is ook goed.",
            "Choose what you want. Choosing nothing is fine too.",
            "Wybierz, co chcesz. Nic nie wybierać też jest w porządku.",
            "Alege ce vrei. Să nu alegi nimic e în regulă.",
            "اختر ما تريد. عدم الاختيار أيضاً جيد.");
        Add("Discovery.Step6.SkipBadge",
            "Overslaan mag", "Skipping is fine", "Można pominąć", "Poți sări", "يمكن التخطي");
        Add("Discovery.Skip",
            "Overslaan", "Skip", "Pomiń", "Sari", "تخطي");
        Add("Discovery.Consent.Eyebrow",
            "Toestemming", "Consent", "Zgoda", "Consimțământ", "موافقة");
        Add("Discovery.Dislike.PrivateNoteEmployersOff",
            "Alleen voor jou en je matches.",
            "Only for you and your matches.",
            "Tylko dla Ciebie i Twoich dopasowań.",
            "Doar pentru tine și potrivirile tale.",
            "لك ولمطابقاتك فقط.");
        Add("Discovery.Step7.Title", "Wat je kunt", "What you can do", "Co potrafisz", "Ce poți face", "ما تستطيع فعله");
        Add("Discovery.Step7.Sub", "Test · 5 vragen", "Test · 5 questions", "Test · 5 pytań", "Test · 5 întrebări", "اختبار · ٥ أسئلة");
        Add("Discovery.Step8.Title", "Beroepen", "Careers", "Zawody", "Meserii", "المهن");
        Add("Discovery.Step8.Sub", "Test · 5 vragen", "Test · 5 questions", "Test · 5 pytań", "Test · 5 întrebări", "اختبار · ٥ أسئلة");
        Add("Discovery.Step9.Title", "Cultuur", "Culture", "Kultura", "Cultură", "الثقافة");
        Add("Discovery.Step9.Sub", "Test · 5 vragen", "Test · 5 questions", "Test · 5 pytań", "Test · 5 întrebări", "اختبار · ٥ أسئلة");
        Add("Discovery.Step10.Title", "Waarden", "Values", "Wartości", "Valori", "القيم");
        Add("Discovery.Step10.Sub", "Test · 5 vragen", "Test · 5 questions", "Test · 5 pytań", "Test · 5 întrebări", "اختبار · ٥ أسئلة");
        Add("Discovery.Say.0",
            "Mijn oude schaal zit wat krap. Zullen we samen kijken wie eronder zit?",
            "My old shell feels a bit tight. Shall we look together at who’s underneath?",
            "Moja stara skorupa jest ciasna. Zerknijmy razem, kto jest pod spodem?",
            "Carapacea mea veche e strâmtă. Ne uităm împreună cine e dedesubt?",
            "صدفتي القديمة ضيقة قليلاً. هل ننظر معاً من تحتها؟");
        Add("Discovery.Say.1",
            "Eerst even kennismaken. Je voeten staan nog in het water.",
            "First a quick hello. Your feet are still in the water.",
            "Najpierw się poznajmy. Twoje stopy nadal są w wodzie.",
            "Mai întâi să ne cunoaștem. Încă ai picioarele în apă.",
            "أولاً نتعارف. قدماك ما زالتا في الماء.");
        Add("Discovery.Say.2",
            "Kies wat nu past. Later aanpassen mag altijd.",
            "Pick what fits now. You can always change it later.",
            "Wybierz to, co pasuje teraz. Później zawsze możesz zmienić.",
            "Alege ce ți se potrivește acum. Poți schimba oricând.",
            "اختر ما يناسبك الآن. يمكنك التعديل لاحقاً دائماً.");
        Add("Discovery.Say.3",
            "Alles wat je deed telt. Ook vrijwilligerswerk, stage of zorgen voor familie.",
            "Everything you did counts. Including volunteering, internships or caring for family.",
            "Wszystko, co robiłeś, się liczy. Także wolontariat, staż czy opieka nad rodziną.",
            "Tot ce ai făcut contează. Inclusiv voluntariat, stagiu sau grija de familie.",
            "كل ما فعلته يُحسب. بما في ذلك التطوع أو التدريب أو رعاية العائلة.");
        Add("Discovery.Say.4",
            "Leren kan op elke leeftijd. Wat wil jij nog leren?",
            "You can learn at any age. What do you still want to learn?",
            "Uczyć się można w każdym wieku. Czego jeszcze chcesz się nauczyć?",
            "Poți învăța la orice vârstă. Ce mai vrei să înveți?",
            "يمكنك التعلم في أي عمر. ماذا تريد أن تتعلم بعد؟");
        Add("Discovery.Say.5",
            "Waar word je blij van? Daar zit vaak je kracht.",
            "What makes you happy? That’s often where your strength is.",
            "Co Cię cieszy? Tam często jest Twoja siła.",
            "Ce te bucură? Acolo e adesea puterea ta.",
            "ما يسعدك؟ هناك غالباً تكون قوتك.");
        Add("Discovery.Say.6",
            "Hier is het rustig tussen de stenen. Je hoeft niets uit te leggen.",
            "It’s calm here between the stones. You don’t have to explain anything.",
            "Tu spokojnie między kamieniami. Nie musisz niczego tłumaczyć.",
            "E liniște aici între pietre. Nu trebuie să explici nimic.",
            "الهدوء هنا بين الحجارة. لا تحتاج لشرح أي شيء.");
        Add("Discovery.Say.7",
            "Er zijn geen foute antwoorden. Kies wat het eerst in je opkomt.",
            "There are no wrong answers. Pick what comes to mind first.",
            "Nie ma złych odpowiedzi. Wybierz to, co pierwsze Ci przyjdzie.",
            "Nu există răspunsuri greșite. Alege ce îți vine primul în minte.",
            "لا إجابات خاطئة. اختر أول ما يخطر ببالك.");
        Add("Discovery.Say.8",
            "Wat doe je graag? Zwem maar rustig mee.",
            "What do you enjoy doing? Swim along at your own pace.",
            "Co lubisz robić? Płyń spokojnie dalej.",
            "Ce-ți place să faci? Înoată liniștit alături.",
            "ماذا تحب أن تفعل؟ اسبح بهدوء معنا.");
        Add("Discovery.Say.9",
            "Waar voel je je thuis? Nog twee lagen.",
            "Where do you feel at home? Two layers left.",
            "Gdzie czujesz się jak w domu? Jeszcze dwie warstwy.",
            "Unde te simți acasă? Mai sunt două straturi.",
            "أين تشعر بأنك في بيتك؟ طبقتان متبقيتان.");
        Add("Discovery.Say.10",
            "Nog één laag, dan zie je wie je bent.",
            "One more layer, then you’ll see who you are.",
            "Jeszcze jedna warstwa, wtedy zobaczysz kim jesteś.",
            "Încă un strat, apoi vezi cine ești.",
            "طبقة واحدة أخرى، ثم ترى من أنت.");
        Add("Discovery.Say.11",
            "Kijk eens hoe je gegroeid bent. Dit is je nieuwe schaal.",
            "Look how you’ve grown. This is your new shell.",
            "Zobacz, jak urosłeś. To Twoja nowa skorupa.",
            "Uite cum ai crescut. Aceasta e noua ta carapace.",
            "انظر كيف كبرت. هذه صدفتك الجديدة.");

        // 08 — tests, shed, end, overview
        Add("Discovery.Test.Competency.Title",
            "Hoe werk jij?", "How do you work?", "Jak pracujesz?", "Cum lucrezi?", "كيف تعمل؟");
        Add("Discovery.Test.Career.Title",
            "Wat vind je leuk?", "What do you enjoy?", "Co lubisz?", "Ce-ți place?", "ماذا تحب؟");
        Add("Discovery.Test.Culture.Title",
            "Waar voel je je thuis?", "Where do you feel at home?", "Gdzie czujesz się jak w domu?", "Unde te simți acasă?", "أين تشعر بأنك في بيتك؟");
        Add("Discovery.Test.Values.Title",
            "Wat vind je belangrijk?", "What matters to you?", "Co jest dla Ciebie ważne?", "Ce este important pentru tine?", "ما المهم بالنسبة لك؟");
        Add("Discovery.Test.CultureScan.Title",
            "Waar voel je je thuis?", "Where do you feel at home?", "Gdzie czujesz się jak w domu?", "Unde te simți acasă?", "أين تشعر بأنك في بيتك؟");
        Add("Discovery.Test.ValuesScan.Title",
            "Wat vind je belangrijk?", "What matters to you?", "Co jest dla Ciebie ważne?", "Ce este important pentru tine?", "ما المهم بالنسبة لك؟");
        Add("Discovery.Test.Lead",
            "Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden.",
            "How well does this sentence fit you? There are no wrong answers.",
            "Jak bardzo pasuje do Ciebie to zdanie? Nie ma złych odpowiedzi.",
            "Cât de bine ți se potrivește această propoziție? Nu există răspunsuri greșite.",
            "ما مدى ملاءمة هذه الجملة لك؟ لا إجابات خاطئة.");
        Add("Discovery.Test.Hint",
            "Na 5 vragen kun je kiezen: klaar, of dieper met 10 of 25 vragen.",
            "After 5 questions you can choose: done, or go deeper with 10 or 25 questions.",
            "Po 5 pytaniach możesz wybrać: gotowe, albo głębiej z 10 lub 25 pytaniami.",
            "După 5 întrebări poți alege: gata, sau mai profund cu 10 sau 25 de întrebări.",
            "بعد 5 أسئلة يمكنك الاختيار: انتهيت، أو أعمق بـ 10 أو 25 سؤالاً.");
        Add("Discovery.Test.HintCulture",
            "Na 5 vragen kun je kiezen: klaar, of dieper met 10 of alle 18.",
            "After 5 questions you can choose: done, or go deeper with 10 or all 18.",
            "Po 5 pytaniach możesz wybrać: gotowe, albo głębiej z 10 lub wszystkimi 18.",
            "După 5 întrebări poți alege: gata, sau mai profund cu 10 sau toate cele 18.",
            "بعد 5 أسئلة يمكنك الاختيار: انتهيت، أو أعمق بـ 10 أو كل الـ 18.");
        Add("Discovery.Test.Counter",
            "Vraag {0} van {1}", "Question {0} of {1}", "Pytanie {0} z {1}", "Întrebarea {0} din {1}", "سؤال {0} من {1}");
        Add("Discovery.TestOf",
            "test {0} van 4", "test {0} of 4", "test {0} z 4", "testul {0} din 4", "اختبار {0} من 4");

        Add("Discovery.Shed.EyebrowDone",
            "stap {0} van 10 klaar", "step {0} of 10 done", "krok {0} z 10 gotowy", "pasul {0} din 10 gata", "الخطوة {0} من 10 جاهزة");
        Add("Discovery.Shed.LayerLabel",
            "Laag {0} van 10", "Layer {0} of 10", "Warstwa {0} z 10", "Strat {0} din 10", "طبقة {0} من 10");
        Add("Discovery.Shed.Title",
            "Weer een laag eraf", "Another layer off", "Znowu warstwa mniej", "Încă un strat jos", "طبقة أخرى سقطت");
        Add("Discovery.Shed.Lead",
            "{0} is klaar. Dit staat nu in je paspoort:",
            "{0} is done. This is now in your passport:",
            "{0} gotowe. To jest teraz w Twoim paszporcie:",
            "{0} e gata. Asta e acum în pașaportul tău:",
            "{0} جاهز. هذا الآن في جوازك:");
        Add("Discovery.Shed.Provisional",
            "(voorlopig)", "(provisional)", "(tymczasowo)", "(provizoriu)", "(مؤقت)");
        Add("Discovery.Shed.DeeperLegend",
            "Wil je dieper in deze test?",
            "Want to go deeper in this test?",
            "Chcesz wejść głębiej w ten test?",
            "Vrei să mergi mai adânc în acest test?",
            "هل تريد التعمق أكثر في هذا الاختبار؟");
        Add("Discovery.Shed.DeeperSub",
            "Meer vragen geeft een scherper beeld. Het mag, het hoeft niet. Je kunt dit later ook nog doen.",
            "More questions give a sharper picture. You may, you don’t have to. You can still do this later.",
            "Więcej pytań daje ostrzejszy obraz. Możesz, nie musisz. Możesz to zrobić później.",
            "Mai multe întrebări dau o imagine mai clară. Poți, nu trebuie. Poți face asta și mai târziu.",
            "المزيد من الأسئلة يعطي صورة أوضح. يمكنك، ولست مضطراً. يمكنك فعل ذلك لاحقاً أيضاً.");
        Add("Discovery.Shed.Keep",
            "Zo laten", "Leave it", "Zostaw tak", "Lasă așa", "اتركه هكذا");
        Add("Discovery.Shed.KeepSub",
            "5 vragen · klaar", "5 questions · done", "5 pytań · gotowe", "5 întrebări · gata", "5 أسئلة · جاهز");
        Add("Discovery.Shed.DoneSub",
            "Gedaan", "Done", "Gotowe", "Gata", "جاهز");
        Add("Discovery.Shed.Deeper",
            "Iets dieper", "A bit deeper", "Trochę głębiej", "Un pic mai adânc", "أعمق قليلاً");
        Add("Discovery.Shed.DeeperSub10",
            "10 vragen · + 2 min", "10 questions · + 2 min", "10 pytań · + 2 min", "10 întrebări · + 2 min", "10 أسئلة · + 2 دقائق");
        Add("Discovery.Shed.Deepest",
            "Heel diep", "Very deep", "Bardzo głęboko", "Foarte adânc", "عميق جداً");
        Add("Discovery.Shed.DeepestSub",
            "25 vragen · + 6 min", "25 questions · + 6 min", "25 pytań · + 6 min", "25 întrebări · + 6 min", "25 سؤالاً · + 6 دقائق");
        Add("Discovery.Shed.DeepestSubCulture",
            "alle 18 · + 4 min", "all 18 · + 4 min", "wszystkie 18 · + 4 min", "toate 18 · + 4 min", "كل الـ 18 · + 4 دقائق");
        Add("Discovery.Shed.FullyDone",
            "Deze test heb je helemaal gedaan.",
            "You’ve fully completed this test.",
            "Ten test masz całkowicie zrobiony.",
            "Ai terminat complet acest test.",
            "لقد أكملت هذا الاختبار بالكامل.");
        Add("Discovery.Shed.DiveDeeper",
            "Duik dieper", "Dive deeper", "Zanurz się głębiej", "Scufundă-te mai adânc", "اغص أعمق");
        Add("Discovery.Shed.NextTest",
            "Verder naar {0}", "Continue to {0}", "Dalej do {0}", "Mai departe la {0}", "متابعة إلى {0}");
        Add("Discovery.Shed.ToLight",
            "Naar het licht", "To the light", "Ku światłu", "Spre lumină", "نحو الضوء");
        Add("Discovery.Shed.Lobster",
            "Voel je dat? Weer een laag eraf. Je wordt groter.",
            "Feel that? Another layer off. You’re growing.",
            "Czujesz to? Znowu warstwa mniej. Rośniesz.",
            "Simți asta? Încă un strat jos. Crești.",
            "هل تشعر بذلك؟ طبقة أخرى سقطت. أنت تكبر.");
        Add("Discovery.Shed.AriaLive",
            "Laag {0} van 10 eraf",
            "Layer {0} of 10 off",
            "Warstwa {0} z 10 spadła",
            "Stratul {0} din 10 jos",
            "سقطت الطبقة {0} من 10");

        Add("Discovery.End.Eyebrow",
            "Klaar · 10 van 10 lagen eraf",
            "Done · 10 of 10 layers off",
            "Gotowe · 10 z 10 warstw mniej",
            "Gata · 10 din 10 straturi jos",
            "جاهز · 10 من 10 طبقات سقطت");
        Add("Discovery.End.PartialEyebrow",
            "Tot hier: 6 van 10 lagen eraf",
            "This far: 6 of 10 layers off",
            "Dotąd: 6 z 10 warstw mniej",
            "Până aici: 6 din 10 straturi jos",
            "حتى هنا: 6 من 10 طبقات سقطت");
        Add("Discovery.End.Title",
            "Dit ben jij, {0}", "This is you, {0}", "To Ty, {0}", "Acesta ești tu, {0}", "هذا أنت، {0}");
        Add("Discovery.End.PartialTitle",
            "Tot hier, {0}", "This far, {0}", "Dotąd, {0}", "Până aici, {0}", "حتى هنا، {0}");
        Add("Discovery.End.Lead",
            "Je oude schaal is eraf. Alles wat je vertelde staat nu in je paspoort. Werkgevers zien pas iets als jij dat wilt.",
            "Your old shell is off. Everything you shared is now in your passport. Employers only see something when you want that.",
            "Twoja stara skorupa spadła. Wszystko, co powiedziałeś, jest teraz w paszporcie. Pracodawcy zobaczą coś dopiero, gdy Ty chcesz.",
            "Vechea ta carapace e jos. Tot ce ai spus e acum în pașaport. Angajatorii văd ceva doar când vrei tu.",
            "صدفتك القديمة سقطت. كل ما قلته موجود الآن في جوازك. أصحاب العمل لا يرون شيئاً إلا عندما تريد أنت.");
        Add("Discovery.End.LeadEmployersOff",
            "Je oude schaal is eraf. Alles wat je vertelde staat nu in je paspoort.",
            "Your old shell is off. Everything you shared is now in your passport.",
            "Twoja stara skorupa spadła. Wszystko, co powiedziałeś, jest teraz w paszporcie.",
            "Vechea ta carapace e jos. Tot ce ai spus e acum în pașaport.",
            "صدفتك القديمة سقطت. كل ما قلته موجود الآن في جوازك.");
        Add("Discovery.End.PartialLead",
            "De tests doe je later, via De ontdekkingsreis.",
            "You can do the tests later via The discovery journey.",
            "Testy zrobisz później przez Odkrywczą podróż.",
            "Testele le faci mai târziu prin Călătoria de descoperire.",
            "يمكنك إجراء الاختبارات لاحقاً عبر رحلة الاكتشاف.");
        Add("Discovery.End.Fact.Strength",
            "Je sterkste punt", "Your strongest point", "Twój najmocniejszy punkt", "Punctul tău forte", "أقوى نقطة لديك");
        Add("Discovery.End.Fact.Work",
            "Werk dat bij je past", "Work that fits you", "Praca, która do Ciebie pasuje", "Muncă care ți se potrivește", "عمل يناسبك");
        Add("Discovery.End.Fact.Value",
            "Belangrijk voor jou", "Important to you", "Ważne dla Ciebie", "Important pentru tine", "مهم لك");
        Add("Discovery.End.FirstImpression",
            "Eerste indruk", "First impression", "Pierwsze wrażenie", "Prima impresie", "انطباع أول");
        Add("Discovery.End.NotDiscovered",
            "Nog niet ontdekt", "Not discovered yet", "Jeszcze nieodkryte", "Încă nedescoperit", "لم يُكتشف بعد");
        Add("Discovery.End.HintMatches",
            "{0} vacatures passen al bij je. Hoe dieper je duikt, hoe scherper je matches.",
            "{0} vacancies already fit you. The deeper you dive, the sharper your matches.",
            "{0} ofert już do Ciebie pasuje. Im głębiej nurkujesz, tym ostrzejsze dopasowania.",
            "{0} joburi ți se potrivesc deja. Cu cât te scufunzi mai adânc, cu atât potrivirile sunt mai clare.",
            "{0} وظائف تناسبك بالفعل. كلما غصت أعمق، صارت مطابقاتك أدق.");
        Add("Discovery.End.HintFirst",
            "Eerste indruk. Hoe dieper je duikt, hoe scherper het beeld.",
            "First impression. The deeper you dive, the sharper the picture.",
            "Pierwsze wrażenie. Im głębiej nurkujesz, tym ostrzejszy obraz.",
            "Prima impresie. Cu cât te scufunzi mai adânc, cu atât imaginea e mai clară.",
            "انطباع أول. كلما غصت أعمق، صارت الصورة أوضح.");
        Add("Discovery.End.HintDeeper",
            "Iets dieper. Hoe dieper je duikt, hoe scherper het beeld.",
            "A bit deeper. The deeper you dive, the sharper the picture.",
            "Trochę głębiej. Im głębiej nurkujesz, tym ostrzejszy obraz.",
            "Un pic mai adânc. Cu cât te scufunzi mai adânc, cu atât imaginea e mai clară.",
            "أعمق قليلاً. كلما غصت أعمق، صارت الصورة أوضح.");
        Add("Discovery.End.HintFull",
            "Helemaal gedaan. Dit beeld staat in je paspoort.",
            "Fully done. This picture is in your passport.",
            "Całkowicie zrobione. Ten obraz jest w Twoim paszporcie.",
            "Complet terminat. Această imagine este în pașaportul tău.",
            "مكتمل بالكامل. هذه الصورة في جوازك.");
        Add("Discovery.End.Deepen",
            "Een test verdiepen", "Deepen a test", "Pogłęb test", "Aprofundează un test", "تعميق اختبار");
        Add("Discovery.End.ViewPassport",
            "Bekijk je paspoort", "View your passport", "Zobacz swój paszport", "Vezi pașaportul", "اعرض جوازك");
        Add("Discovery.End.PassportAria",
            "Voorbeeld van je paspoort", "Preview of your passport", "Podgląd paszportu", "Previzualizare pașaport", "معاينة جوازك");

        Add("Discovery.Overview.Title",
            "Verder ontdekken", "Keep exploring", "Odkrywaj dalej", "Descoperă mai departe", "واصل الاكتشاف");
        Add("Discovery.Overview.Lead",
            "Kijk terug op je reis, of duik dieper.",
            "Look back on your journey, or dive deeper.",
            "Spójrz wstecz na podróż albo zanurz się głębiej.",
            "Privește înapoi la călătorie, sau scufundă-te mai adânc.",
            "انظر إلى رحلتك، أو اغص أعمق.");
        Add("Discovery.Overview.TestsHeading",
            "Je tests", "Your tests", "Twoje testy", "Testele tale", "اختباراتك");
        Add("Discovery.Overview.Done",
            "Klaar", "Done", "Gotowe", "Gata", "جاهز");
        Add("Discovery.Overview.Skipped",
            "Overgeslagen", "Skipped", "Pominięte", "Omise", "تم التخطي");
        Add("Discovery.Overview.New",
            "Nieuw · 1 min", "New · 1 min", "Nowe · 1 min", "Nou · 1 min", "جديد · دقيقة");
        Add("Discovery.Overview.View",
            "Bekijken", "View", "Zobacz", "Vezi", "عرض");
        Add("Discovery.Overview.Save",
            "Opslaan", "Save", "Zapisz", "Salvează", "حفظ");
        Add("Discovery.Overview.Back",
            "Terug naar overzicht", "Back to overview", "Wróć do przeglądu", "Înapoi la prezentare", "العودة إلى النظرة العامة");
        Add("Discovery.Overview.FullyDone",
            "Helemaal gedaan", "Fully done", "Całkowicie zrobione", "Complet terminat", "مكتمل بالكامل");
        Add("Discovery.Overview.FullReport",
            "Uitgebreid rapport", "Full report", "Pełny raport", "Raport complet", "تقرير مفصل");
        Add("Discovery.Overview.Depth",
            "{0} van {1} vragen", "{0} of {1} questions", "{0} z {1} pytań", "{0} din {1} întrebări", "{0} من {1} أسئلة");
        Add("Discovery.Overview.All18",
            "Alle 18", "All 18", "Wszystkie 18", "Toate 18", "كل الـ 18");
        Add("Discovery.Overview.Consent",
            "Toestemming voor de tests",
            "Consent for the tests",
            "Zgoda na testy",
            "Consimțământ pentru teste",
            "الموافقة على الاختبارات");
        Add("Discovery.Overview.GiveConsent",
            "Nu geven", "Give now", "Daj teraz", "Acordă acum", "قدّم الآن");
        Add("Discovery.Consent.Decline",
            "Zonder tests afronden",
            "Finish without tests",
            "Zakończ bez testów",
            "Finalizează fără teste",
            "إنهاء بدون اختبارات");
        Add("Discovery.Rail.DoneCountShort",
            "Klaar · 10 van 10", "Done · 10 of 10", "Gotowe · 10 z 10", "Gata · 10 din 10", "جاهز · 10 من 10");

        Add("MatchUnlock.ContinueStart",
            "Verder met starten", "Continue starting", "Kontynuuj start", "Continuă pornirea", "متابعة البدء");
        Add("MatchUnlock.StartOrResume",
            "Start of hervat", "Start or resume", "Start lub wznów", "Pornește sau reia", "ابدأ أو استأنف");
    }
}
