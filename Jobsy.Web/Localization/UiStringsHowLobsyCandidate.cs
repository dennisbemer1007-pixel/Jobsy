namespace Jobsy.Web.Localization;

/// <summary>
/// Copy for <c>/candidate/hoe-werkt-lobsy</c> — the five stones in the journey style (05 §3).
/// Separate from <c>HowLobsy.*</c>, which stays with the guest/role guides on <c>/hoe-werkt-lobsy</c>.
/// </summary>
public static class UiStringsHowLobsyCandidate
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

        Add("HowC.Eyebrow",
            "Hoe werkt Lobsy?",
            "How does Lobsy work?",
            "Jak działa Lobsy?",
            "Cum funcționează Lobsy?",
            "كيف يعمل لوبسي؟");
        Add("HowC.Title",
            "{0} stenen, in je eigen tempo",
            "{0} stones, at your own pace",
            "{0} kamienie, we własnym tempie",
            "{0} pietre, în ritmul tău",
            "{0} أحجار، بالسرعة التي تريدها");
        Add("HowC.Lead",
            "Je hoeft niet alles tegelijk. Begin waar je wilt. Alles wordt bewaard.",
            "You don’t have to do it all at once. Start where you like. Everything is saved.",
            "Nie musisz robić wszystkiego naraz. Zacznij tam, gdzie chcesz. Wszystko jest zapisywane.",
            "Nu trebuie să faci totul dintr-odată. Începe unde vrei. Totul se salvează.",
            "لا حاجة لفعل كل شيء مرة واحدة. ابدأ من حيث تريد. كل شيء محفوظ.");
        Add("HowC.List.Label",
            "Zo werkt Lobsy",
            "How Lobsy works",
            "Tak działa Lobsy",
            "Așa funcționează Lobsy",
            "هكذا يعمل لوبسي");

        // Spelled-out counts keep the h1 honest when a stone is hidden by a flag.
        Add("HowC.Count.1", "Eén", "One", "Jeden", "O", "حجر واحد");
        Add("HowC.Count.2", "Twee", "Two", "Dwa", "Două", "حجران");
        Add("HowC.Count.3", "Drie", "Three", "Trzy", "Trei", "ثلاثة");
        Add("HowC.Count.4", "Vier", "Four", "Cztery", "Patru", "أربعة");
        Add("HowC.Count.5", "Vijf", "Five", "Pięć", "Cinci", "خمسة");

        Add("HowC.Stone.Discovery.Title",
            "De ontdekkingsreis",
            "The discovery journey",
            "Podróż odkrywcza",
            "Călătoria de descoperire",
            "رحلة الاستكشاف");
        Add("HowC.Stone.Discovery.Body",
            "Ontdek wie je bent. Korte stappen, in je eigen tempo.",
            "Discover who you are. Short steps, at your own pace.",
            "Odkryj, kim jesteś. Krótkie kroki, we własnym tempie.",
            "Descoperă cine ești. Pași scurți, în ritmul tău.",
            "اكتشف من أنت. خطوات قصيرة، بالسرعة التي تريدها.");
        Add("HowC.Stone.Profile.Title",
            "Je profiel invullen",
            "Fill in your profile",
            "Uzupełnij profil",
            "Completează profilul",
            "أكمل ملفك");
        Add("HowC.Stone.Profile.Body",
            "Vertel kort wie je bent. Korte stappen, in je eigen tempo.",
            "Tell us briefly who you are. Short steps, at your own pace.",
            "Powiedz krótko, kim jesteś. Krótkie kroki, we własnym tempie.",
            "Spune scurt cine ești. Pași scurți, în ritmul tău.",
            "أخبرنا باختصار من أنت. خطوات قصيرة، بالسرعة التي تريدها.");
        Add("HowC.Stone.Passport.Title",
            "Mijn Paspoort",
            "My Passport",
            "Mój Paszport",
            "Pașaportul meu",
            "جوازي");
        Add("HowC.Stone.Passport.Body",
            "Alles over jou op één plek. Jij kiest wie het ziet.",
            "Everything about you in one place. You choose who sees it.",
            "Wszystko o Tobie w jednym miejscu. Ty wybierasz, kto to widzi.",
            "Tot despre tine într-un singur loc. Tu alegi cine vede.",
            "كل ما يتعلق بك في مكان واحد. أنت تختار من يراه.");
        Add("HowC.Stone.MyProfile.Title",
            "Mijn profiel",
            "My profile",
            "Mój profil",
            "Profilul meu",
            "ملفي");
        Add("HowC.Stone.MyProfile.Body",
            "Alles over jou op één plek.",
            "Everything about you in one place.",
            "Wszystko o Tobie w jednym miejscu.",
            "Tot despre tine într-un singur loc.",
            "كل ما يتعلق بك في مكان واحد.");
        Add("HowC.Stone.Career.Title",
            "Carrière",
            "Career",
            "Kariera",
            "Carieră",
            "المسار المهني");
        Add("HowC.Stone.Career.Body",
            "Kies je droombaan en groei steen voor steen.",
            "Pick your dream job and grow stone by stone.",
            "Wybierz wymarzoną pracę i rośnij kamień po kamieniu.",
            "Alege jobul de vis și crește pas cu pas.",
            "اختر وظيفة أحلامك وانمُ حجراً بعد حجر.");
        Add("HowC.Stone.JobMap.Title",
            "Banenkaart",
            "Job map",
            "Mapa ofert",
            "Harta joburilor",
            "خريطة الوظائف");
        Add("HowC.Stone.JobMap.Body",
            "Vind werk dichtbij huis, op de kaart.",
            "Find work close to home, on the map.",
            "Znajdź pracę blisko domu, na mapie.",
            "Găsește muncă aproape de casă, pe hartă.",
            "اعثر على عمل قريب من بيتك، على الخريطة.");
        Add("HowC.Stone.Applications.Title",
            "Sollicitaties",
            "Applications",
            "Aplikacje",
            "Candidaturi",
            "الطلبات");
        Add("HowC.Stone.Applications.Body",
            "Zie waar je solliciteerde en wat er gebeurt.",
            "See where you applied and what happens next.",
            "Zobacz, gdzie aplikowałeś i co się dzieje.",
            "Vezi unde ai aplicat și ce se întâmplă.",
            "شاهد أين تقدّمت وما يحدث بعد ذلك.");

        // Short scene labels (aria-hidden).
        Add("HowC.Short.Discovery", "Reis", "Journey", "Podróż", "Călătorie", "رحلة");
        Add("HowC.Short.Passport", "Paspoort", "Passport", "Paszport", "Pașaport", "جواز");
        Add("HowC.Short.Career", "Carrière", "Career", "Kariera", "Carieră", "مسار");
        Add("HowC.Short.JobMap", "Banenkaart", "Job map", "Mapa", "Hartă", "خريطة");
        Add("HowC.Short.Applications", "Sollicitaties", "Applications", "Aplikacje", "Candidaturi", "طلبات");

        Add("HowC.Link.Back", "Kijk terug", "Look back", "Zobacz ponownie", "Privește înapoi", "راجعها");
        Add("HowC.Link.Start", "Begin", "Start", "Zacznij", "Începe", "ابدأ");
        Add("HowC.Link.Continue", "Ga verder", "Continue", "Kontynuuj", "Continuă", "تابع");
        Add("HowC.Link.Open", "Open", "Open", "Otwórz", "Deschide", "افتح");

        Add("HowC.State.Done", "Klaar", "Done", "Gotowe", "Gata", "تم");
        Add("HowC.State.Now", "Hier ben je nu", "You are here now", "Tu jesteś teraz", "Ești aici acum", "أنت هنا الآن");
        Add("HowC.State.Todo", "Nog te doen", "Still to do", "Jeszcze do zrobienia", "Încă de făcut", "ما زال أمامك");

        Add("HowC.Safe.Title",
            "Veilig en rustig",
            "Safe and calm",
            "Bezpiecznie i spokojnie",
            "În siguranță și calm",
            "بأمان وهدوء");
        Add("HowC.Safe.TitleMobile",
            "Goed om te weten",
            "Good to know",
            "Warto wiedzieć",
            "Bine de știut",
            "معلومات مفيدة");
        Add("HowC.Safe.Names",
            "Werkgevers zien je naam pas als jij ja zegt.",
            "Employers only see your name once you say yes.",
            "Pracodawcy zobaczą Twoje imię dopiero, gdy się zgodzisz.",
            "Angajatorii îți văd numele doar după ce spui da.",
            "لا يرى أصحاب العمل اسمك إلا عندما توافق.");
        Add("HowC.Safe.Saved",
            "Alles is bewaard. Stoppen mag altijd.",
            "Everything is saved. You can stop whenever you want.",
            "Wszystko jest zapisane. Zawsze możesz przerwać.",
            "Totul este salvat. Poți opri oricând.",
            "كل شيء محفوظ. يمكنك التوقف في أي وقت.");
        Add("HowC.Safe.Languages",
            "Lobsy in jouw taal:",
            "Lobsy in your language:",
            "Lobsy w Twoim języku:",
            "Lobsy în limba ta:",
            "لوبسي بلغتك:");
        Add("HowC.Safe.Assistant",
            "Vragen? Tik op {0}, rechts op het scherm.",
            "Questions? Tap {0}, on the right of the screen.",
            "Pytania? Dotknij {0}, po prawej stronie ekranu.",
            "Întrebări? Atinge {0}, în dreapta ecranului.",
            "أسئلة؟ اضغط على {0}، على يمين الشاشة.");
        Add("HowC.Safe.AssistantRtl",
            "Vragen? Tik op {0}, links op het scherm.",
            "Questions? Tap {0}, on the left of the screen.",
            "Pytania? Dotknij {0}, po lewej stronie ekranu.",
            "Întrebări? Atinge {0}, în stânga ecranului.",
            "أسئلة؟ اضغط على {0}، على يسار الشاشة.");

        Add("HowC.Say",
            "Kijk: {0} stenen heb je al. Nu kies je waar je naartoe groeit.",
            "Look: you already have {0} stones. Now you choose where you grow to.",
            "Patrz: masz już {0} kamienie. Teraz wybierasz, gdzie chcesz rosnąć.",
            "Uite: ai deja {0} pietre. Acum alegi încotro crești.",
            "انظر: لديك بالفعل {0} أحجار. الآن تختار إلى أين تنمو.");
        Add("HowC.Say.Zero",
            "Begin bij de eerste steen. Ik loop met je mee.",
            "Start at the first stone. I’ll walk with you.",
            "Zacznij od pierwszego kamienia. Pójdę z Tobą.",
            "Începe de la prima piatră. Merg cu tine.",
            "ابدأ من الحجر الأول. سأسير معك.");
        Add("HowC.Say.All",
            "Alle stenen heb je gehad. Kijk rustig terug wat je wilt.",
            "You’ve had every stone. Look back at whatever you like.",
            "Przeszedłeś wszystkie kamienie. Spokojnie wróć do tego, co chcesz.",
            "Ai trecut toate pietrele. Privește liniștit înapoi la ce vrei.",
            "لقد مررت بكل الأحجار. راجع بهدوء ما تريد.");
        Add("HowC.Say.Short",
            "{0} stenen heb je al. Op naar steen {1}.",
            "You already have {0} stones. On to stone {1}.",
            "Masz już {0} kamienie. Do kamienia {1}.",
            "Ai deja {0} pietre. Spre piatra {1}.",
            "لديك بالفعل {0} أحجار. إلى الحجر {1}.");

        Add("HowC.Ack",
            "Ik snap het",
            "I get it",
            "Rozumiem",
            "Am înțeles",
            "فهمت");
        Add("HowC.Ack.Done",
            "Top. Je vindt deze uitleg altijd terug in het menu.",
            "Great. You can always find this explanation in the menu.",
            "Super. To wyjaśnienie zawsze znajdziesz w menu.",
            "Super. Găsești oricând această explicație în meniu.",
            "رائع. يمكنك دائماً العثور على هذا الشرح في القائمة.");
        Add("HowC.Primary",
            "Verder met {0}",
            "Continue with {0}",
            "Dalej z {0}",
            "Continuă cu {0}",
            "تابع مع {0}");
        Add("HowC.Err.Save",
            "Dat lukte niet. Probeer het straks opnieuw.",
            "That didn’t work. Please try again later.",
            "Nie udało się. Spróbuj później.",
            "Nu a funcționat. Încearcă mai târziu.",
            "لم ينجح ذلك. حاول لاحقاً.");
    }
}
