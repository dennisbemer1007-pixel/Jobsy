namespace Jobsy.Web.Localization;

/// <summary>UI strings for werkgever-aanmelding (verification, wizard, banners).</summary>
internal static class UiStringsWerkgeverAanmelding
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

        Add("WaBanner.PreviewLine",
            "Voorbeeld · nog niet zichtbaar voor kandidaten",
            "Preview · not yet visible to candidates",
            "Podgląd · jeszcze niewidoczny dla kandydatów",
            "Previzualizare · încă nevăzut de candidați",
            "معاينة · غير مرئي للمرشحين بعد");

        Add("WaBanner.Blocked.Publish",
            "Je bedrijf is nog niet geverifieerd. Publiceren volgt na verificatie.",
            "Your company is not verified yet. Publishing unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Publikacja po weryfikacji.",
            "Firma ta nu este încă verificată. Publicarea urmează după verificare.",
            "شركتك غير موثّقة بعد. النشر بعد التحقق.");

        Add("WaBanner.Blocked.Tokens",
            "Je bedrijf is nog niet geverifieerd. Tokens kopen volgt na verificatie.",
            "Your company is not verified yet. Buying tokens unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Zakup tokenów po weryfikacji.",
            "Firma ta nu este încă verificată. Cumpărarea de tokeni urmează după verificare.",
            "شركتك غير موثّقة بعد. شراء الرموز بعد التحقق.");

        Add("WaBanner.Blocked.Candidates",
            "Je bedrijf is nog niet geverifieerd. Kandidatengegevens volgen na verificatie.",
            "Your company is not verified yet. Candidate data unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Dane kandydatów po weryfikacji.",
            "Firma ta nu este încă verificată. Datele candidaților urmează după verificare.",
            "شركتك غير موثّقة بعد. بيانات المرشحين بعد التحقق.");

        Add("WaBanner.ReadyCta",
            "Klaarzetten · gaat live na verificatie",
            "Mark ready · goes live after verification",
            "Oznacz jako gotowe · start po weryfikacji",
            "Pregătește · apare după verificare",
            "جهّز · يُنشر بعد التحقق");

        Add("WaBanner.ReadyStatus",
            "Klaar · gaat live na verificatie",
            "Ready · goes live after verification",
            "Gotowe · start po weryfikacji",
            "Gata · apare după verificare",
            "جاهز · يُنشر بعد التحقق");

        // Layout / guide / steps
        Add("Wa.Layout.Title", "Bedrijf registreren", "Register company", "Zarejestruj firmę", "Înregistrează firma", "تسجيل الشركة");
        Add("Wa.Layout.HaveAccount", "Al een account?", "Already have an account?", "Masz już konto?", "Ai deja un cont?", "هل لديك حساب؟");
        Add("Wa.Layout.Login", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
        Add("Wa.Guide.Label", "Begeleiding", "Guidance", "Przewodnik", "Ghid", "إرشاد");
        Add("Wa.Guide.Speech1", "Hoi! Ik ben Lobsy. Typ je KvK-nummer of zoek op naam.", "Hi! I'm Lobsy. Type your KvK number or search by name.", "Cześć! Jestem Lobsy. Wpisz numer KvK lub szukaj po nazwie.", "Salut! Sunt Lobsy. Tastează numărul KvK sau caută după nume.", "مرحباً! أنا Lobsy. اكتب رقم KvK أو ابحث بالاسم.");
        Add("Wa.Guide.Speech2", "Mooi bedrijf! Meld je het hele bedrijf aan of alleen een vestiging?", "Nice company! Register the whole company or just one location?", "Świetna firma! Cała firma czy jedna lokalizacja?", "Firmă frumoasă! Toată firma sau o singură locație?", "شركة رائعة! كل الشركة أم موقع واحد؟");
        Add("Wa.Guide.Speech3", "Bijna een account! Gebruik je werkmail.", "Almost there! Use your work e-mail.", "Prawie gotowe! Użyj służbowego e-maila.", "Aproape gata! Folosește e-mailul de serviciu.", "ほぼ جاهز! استخدم بريد العمل.");
        Add("Wa.Guide.SpeechCode", "We hebben een code gestuurd. Vul die hier in.", "We sent a code. Enter it here.", "Wysłaliśmy kod. Wpisz go tutaj.", "Am trimis un cod. Introdu-l aici.", "أرسلنا رمزاً. أدخله هنا.");
        Add("Wa.Steps.Label", "Stappen", "Steps", "Kroki", "Pași", "الخطوات");
        Add("Wa.Steps.Optional", "optioneel", "optional", "opcjonalne", "opțional", "اختياري");
        Add("Wa.Steps.1.Title", "Bedrijf zoeken", "Find company", "Szukaj firmy", "Caută firma", "البحث عن الشركة");
        Add("Wa.Steps.1.Sub", "KvK-nummer of naam", "KvK number or name", "Numer KvK lub nazwa", "Număr KvK sau nume", "رقم KvK أو الاسم");
        Add("Wa.Steps.2.Title", "Vestiging kiezen", "Choose location", "Wybierz lokalizację", "Alege locația", "اختر الموقع");
        Add("Wa.Steps.2.Sub", "Heel bedrijf of één vestiging", "Whole company or one location", "Cała firma lub jedna lokalizacja", "Toată firma sau o locație", "كل الشركة أو موقع واحد");
        Add("Wa.Steps.3.Title", "Jouw account", "Your account", "Twoje konto", "Contul tău", "حسابك");
        Add("Wa.Steps.3.Sub", "Gegevens en inloggen", "Details and sign-in", "Dane i logowanie", "Date și autentificare", "البيانات وتسجيل الدخول");
        Add("Wa.Steps.4.Title", "Over je bedrijf", "About your company", "O firmie", "Despre firmă", "عن شركتك");
        Add("Wa.Steps.4.Sub", "Branche, cultuur, betrokkenheid", "Industry, culture, engagement", "Branża, kultura, zaangażowanie", "Industrie, cultură, implicare", "القطاع والثقافة والمشاركة");
        Add("Wa.Steps.5.Title", "Verifiëren", "Verify", "Weryfikacja", "Verificare", "التحقق");
        Add("Wa.Steps.5.Sub", "Zakelijk e-mailadres of brief", "Business e-mail or letter", "E-mail firmowy lub list", "E-mail de firmă sau scrisoare", "بريد عمل أو خطاب");

        // Search
        Add("Wa.Search.Eyebrow", "Welkom bij Lobsy voor werkgevers", "Welcome to Lobsy for employers", "Witamy pracodawców w Lobsy", "Bine ați venit angajatorii pe Lobsy", "مرحباً بأصحاب العمل في Lobsy");
        Add("Wa.Search.Title", "Laten we je bedrijf vinden", "Let's find your company", "Znajdźmy Twoją firmę", "Hai să găsim firma ta", "لنجد شركتك");
        Add("Wa.Search.Lead", "Typ je KvK-nummer of zoek op naam. Staat je bedrijf erbij? Dan zie je meteen al je vestigingen.", "Type your KvK number or search by name. If we find your company, you will see all locations.", "Wpisz numer KvK lub szukaj po nazwie. Jeśli znajdziemy firmę, zobaczysz lokalizacje.", "Tastează numărul KvK sau caută după nume. Dacă găsim firma, vezi locațiile.", "اكتب رقم KvK أو ابحث بالاسم. إن وجدنا شركتك ترى المواقع.");
        Add("Wa.Search.QueryLabel", "Bedrijfsnaam of KvK-nummer", "Company name or KvK number", "Nazwa firmy lub numer KvK", "Numele firmei sau numărul KvK", "اسم الشركة أو رقم KvK");
        Add("Wa.Search.QueryPlaceholder", "90123456 of bedrijfsnaam", "90123456 or company name", "90123456 lub nazwa firmy", "90123456 sau numele firmei", "90123456 أو اسم الشركة");
        Add("Wa.Search.PlaceLabel", "Plaats (optioneel)", "City (optional)", "Miejscowość (opcjonalnie)", "Localitate (opțional)", "المدينة (اختياري)");
        Add("Wa.Search.PlacePlaceholder", "Bijv. Utrecht", "E.g. Utrecht", "Np. Utrecht", "Ex. Utrecht", "مثل Utrecht");
        Add("Wa.Search.Button", "Zoeken →", "Search →", "Szukaj →", "Caută →", "بحث →");
        Add("Wa.Search.Hint", "8 cijfers? Dan zoeken we direct op KvK-nummer. Anders zoeken we op naam (vanaf 3 letters).", "8 digits? We search by KvK number. Otherwise by name (from 3 letters).", "8 cyfr? Szukamy po numerze KvK. Inaczej po nazwie (od 3 liter).", "8 cifre? Căutăm după numărul KvK. Altfel după nume (de la 3 litere).", "8 أرقام؟ نبحث برقم KvK. وإلا بالاسم (من 3 أحرف).");
        Add("Wa.Search.MinChars", "Typ minstens 3 tekens om te zoeken op naam.", "Type at least 3 characters to search by name.", "Wpisz co najmniej 3 znaki, aby szukać po nazwie.", "Tastează cel puțin 3 caractere pentru căutare după nume.", "اكتب 3 أحرف على الأقل للبحث بالاسم.");
        Add("Wa.Search.ResultsLabel", "Zoekresultaten", "Search results", "Wyniki wyszukiwania", "Rezultatele căutării", "نتائج البحث");
        Add("Wa.Search.ResultsCount", "{0} resultaten uit het KVK Handelsregister", "{0} results from the KVK Trade Register", "{0} wyników z rejestru KVK", "{0} rezultate din registrul KVK", "{0} نتيجة من سجل KVK");
        Add("Wa.Search.OneBranch", "{0} vestiging", "{0} location", "{0} lokalizacja", "{0} locație", "{0} موقع");
        Add("Wa.Search.NBranches", "{0} vestigingen", "{0} locations", "{0} lokalizacje", "{0} locații", "{0} مواقع");
        Add("Wa.Search.BranchSingular", "vestiging", "location", "lokalizacja", "locație", "موقع");
        Add("Wa.Search.BranchPlural", "vestigingen", "locations", "lokalizacje", "locații", "مواقع");
        Add("Wa.Search.AlreadyOnLobsy", "Al op Lobsy", "Already on Lobsy", "Już na Lobsy", "Deja pe Lobsy", "موجود على Lobsy");
        Add("Wa.Search.Unavailable", "KVK-dienst is tijdelijk niet beschikbaar. Vul je vestiging handmatig in.", "The KVK service is temporarily unavailable. Enter your location manually.", "Usługa KVK jest chwilowo niedostępna. Wprowadź lokalizację ręcznie.", "Serviciul KVK este temporar indisponibil. Introdu locația manual.", "خدمة KVK غير متاحة مؤقتاً. أدخل موقعك يدوياً.");
        Add("Wa.Search.NoResults", "Geen resultaten. Probeer je KvK-nummer of registreer handmatig.", "No results. Try your KvK number or register manually.", "Brak wyników. Spróbuj numeru KvK lub zarejestruj ręcznie.", "Niciun rezultat. Încearcă numărul KvK sau înregistrează manual.", "لا نتائج. جرّب رقم KvK أو سجّل يدوياً.");
        Add("Wa.Search.NeedTitle", "Wat heb je nodig?", "What do you need?", "Czego potrzebujesz?", "De ce ai nevoie?", "ماذا تحتاج؟");
        Add("Wa.Search.Need1", "Je KvK-nummer of bedrijfsnaam. Wij halen de rest op uit het Handelsregister.", "Your KvK number or company name. We fetch the rest from the Trade Register.", "Numer KvK lub nazwa firmy. Resztę pobierzemy z rejestru.", "Numărul KvK sau numele firmei. Restul îl luăm din registru.", "رقم KvK أو اسم الشركة. نجلب الباقي من السجل.");
        Add("Wa.Search.Need2", "Een zakelijk e-mailadres. Daarmee verifieer je het snelst.", "A business e-mail. That verifies you the fastest.", "Firmowy e-mail. Najszybsza weryfikacja.", "E-mail de firmă. Verificare rapidă.", "بريد عمل. أسرع تحقق.");
        Add("Wa.Search.Need3", "Zo'n 5 minuten. Over je bedrijf kun je later invullen.", "About 5 minutes. You can fill company details later.", "Około 5 minut. Dane firmy uzupełnisz później.", "Cam 5 minute. Detaliile firmei le poți completa mai târziu.", "حوالي 5 دقائق. يمكنك إكمال بيانات الشركة لاحقاً.");
        Add("Wa.Search.Need4", "Veilig en eerlijk. Kandidaten zien je pas na verificatie.", "Safe and fair. Candidates see you only after verification.", "Bezpiecznie i uczciwie. Kandydaci zobaczą Cię po weryfikacji.", "Sigur și corect. Candidații te văd după verificare.", "آمن وعادل. المرشحون يرونك بعد التحقق.");
        Add("Wa.Search.ManualLead", "Staat je bedrijf er niet bij?", "Company not listed?", "Nie ma Twojej firmy?", "Firma nu apare?", "شركتك غير ظاهرة؟");
        Add("Wa.Search.ManualCta", "Registreer handmatig", "Register manually", "Zarejestruj ręcznie", "Înregistrează manual", "سجّل يدوياً");

        Add("Wa.Manual.Name", "Vestigingsnaam", "Location name", "Nazwa lokalizacji", "Numele locației", "اسم الموقع");
        Add("Wa.Manual.Kvk", "KvK-nummer", "KvK number", "Numer KvK", "Număr KvK", "رقم KvK");
        Add("Wa.Manual.Address", "Adres", "Address", "Adres", "Adresă", "العنوان");
        Add("Wa.Manual.PostcodePlace", "Postcode en plaats", "Postcode and city", "Kod pocztowy i miejscowość", "Cod poștal și localitate", "الرمز البريدي والمدينة");

        // Scope
        Add("Wa.Scope.Eyebrow", "Stap 2 · Vestiging kiezen", "Step 2 · Choose location", "Krok 2 · Wybierz lokalizację", "Pasul 2 · Alege locația", "الخطوة 2 · اختر الموقع");
        Add("Wa.Scope.Title", "Gevonden! Wat wil je aanmelden?", "Found! What do you want to register?", "Znaleziono! Co chcesz zarejestrować?", "Găsit! Ce vrei să înregistrezi?", "وجدناها! ماذا تريد تسجيله؟");
        Add("Wa.Scope.OtherCompany", "Ander bedrijf", "Other company", "Inna firma", "Altă firmă", "شركة أخرى");
        Add("Wa.Scope.WhatTitle", "Wat meld je aan?", "What are you registering?", "Co rejestrujesz?", "Ce înregistrezi?", "ماذا تسجّل؟");
        Add("Wa.Scope.WholeCompany", "Heel het bedrijf", "The whole company", "Cała firma", "Toată firma", "كل الشركة");
        Add("Wa.Scope.WholeCompanyBody", "Jij wordt bedrijfsmanager. Alle vrije vestigingen komen er meteen bij.", "You become company manager. All free locations are included.", "Zostajesz menedżerem firmy. Wolne lokalizacje są od razu dodane.", "Devii manager de firmă. Locațiile libere sunt incluse.", "تصبح مدير الشركة. تُضاف المواقع الحرة فوراً.");
        Add("Wa.Scope.SomeBranches", "Eén of een paar vestigingen", "One or a few locations", "Jedna lub kilka lokalizacji", "Una sau câteva locații", "موقع واحد أو عدة مواقع");
        Add("Wa.Scope.SomeBranchesBody", "Handig als je alleen voor jouw locatie werft. Uitbreiden kan later altijd.", "Handy if you only recruit for your location. You can expand later.", "Przydatne, gdy rekrutujesz tylko dla swojej lokalizacji. Rozszerzysz później.", "Util dacă recrutezi doar pentru locația ta. Poți extinde ulterior.", "مفيد إن كنت توظّف لموقعك فقط. يمكنك التوسع لاحقاً.");
        Add("Wa.Scope.BranchesTitle", "Vestigingen van dit bedrijf", "Locations of this company", "Lokalizacje firmy", "Locațiile firmei", "مواقع هذه الشركة");
        Add("Wa.Scope.HeadOffice", "Hoofdvestiging", "Head office", "Siedziba główna", "Sediu principal", "المقر الرئيسي");
        Add("Wa.Scope.AlreadyManaged", "Heeft al een beheerder", "Already has a manager", "Ma już administratora", "Are deja un administrator", "له مدير بالفعل");
        Add("Wa.Scope.Available", "Beschikbaar", "Available", "Dostępna", "Disponibilă", "متاح");
        Add("Wa.Scope.RequestAccess", "Vraag toegang aan", "Request access", "Poproś o dostęp", "Solicită acces", "اطلب الوصول");
        Add("Wa.Scope.OwnedWarning", "Sommige vestigingen hebben al een beheerder. Die nemen we niet mee. Werk je daar? Vraag toegang aan.", "Some locations already have a manager. Those are skipped. Work there? Request access.", "Niektóre lokalizacje mają już administratora. Te pomijamy. Pracujesz tam? Poproś o dostęp.", "Unele locații au deja un administrator. Le omitem. Lucrezi acolo? Solicită acces.", "لبعض المواقع مدير بالفعل. نتخطاها. هل تعمل هناك؟ اطلب الوصول.");
        Add("Wa.Scope.AlreadyRegistered", "Dit bedrijf staat al op Lobsy", "This company is already on Lobsy", "Ta firma jest już na Lobsy", "Această firmă este deja pe Lobsy", "هذه الشركة موجودة على Lobsy");
        Add("Wa.Scope.AlreadyRegisteredBody", "Je kunt toegang vragen aan de bedrijfsmanager, of eigendom overnemen via brief plus admin.", "You can request access from the company manager, or transfer ownership via letter plus admin.", "Możesz poprosić o dostęp menedżera firmy albo przejąć własność listem i adminem.", "Poți solicita acces managerului de firmă sau transferul proprietății prin scrisoare și admin.", "يمكنك طلب الوصول من مدير الشركة أو نقل الملكية عبر خطاب ومسؤول.");
        Add("Wa.Scope.OwnershipTransfer", "Ik ben de eigenaar en er beheert iemand anders", "I am the owner and someone else manages it", "Jestem właścicielem, a ktoś inny zarządza", "Sunt proprietarul și altcineva administrează", "أنا المالك ويديرها شخص آخر");
        Add("Wa.Scope.Continue", "Verder naar je account →", "Continue to your account →", "Dalej do konta →", "Continuă la cont →", "المتابعة إلى حسابك →");
        Add("Wa.Scope.PickOne", "Kies minstens één beschikbare vestiging.", "Pick at least one available location.", "Wybierz co najmniej jedną dostępną lokalizację.", "Alege cel puțin o locație disponibilă.", "اختر موقعاً متاحاً واحداً على الأقل.");

        // Account
        Add("Wa.Account.Eyebrow", "Stap 3 · Jouw account", "Step 3 · Your account", "Krok 3 · Twoje konto", "Pasul 3 · Contul tău", "الخطوة 3 · حسابك");
        Add("Wa.Account.Title", "Nu even over jou", "Now about you", "Teraz o Tobie", "Acum despre tine", "الآن عنك");
        Add("Wa.Account.Lead", "Jij wordt de bedrijfsmanager van {0}. Dat kun je later aan een collega overdragen.", "You become the company manager of {0}. You can hand this over to a colleague later.", "Zostajesz menedżerem firmy {0}. Później możesz przekazać to koledze.", "Devii managerul firmei {0}. Poți transfera ulterior unui coleg.", "تصبح مدير شركة {0}. يمكنك نقل ذلك لاحقاً لزميل.");
        Add("Wa.Account.Name", "Voor- en achternaam", "Full name", "Imię i nazwisko", "Numele complet", "الاسم الكامل");
        Add("Wa.Account.Function", "Functie (optioneel)", "Job title (optional)", "Stanowisko (opcjonalnie)", "Funcție (opțional)", "المسمى الوظيفي (اختياري)");
        Add("Wa.Account.Email", "Zakelijk e-mailadres", "Business e-mail", "Firmowy e-mail", "E-mail de firmă", "بريد العمل");
        Add("Wa.Account.Phone", "Telefoon (optioneel)", "Phone (optional)", "Telefon (opcjonalnie)", "Telefon (opțional)", "الهاتف (اختياري)");
        Add("Wa.Account.PhoneHint", "Alleen voor support. Nooit zichtbaar voor kandidaten.", "Support only. Never visible to candidates.", "Tylko do wsparcia. Niewidoczny dla kandydatów.", "Doar pentru suport. Invizibil pentru candidați.", "للدعم فقط. غير ظاهر للمرشحين.");
        Add("Wa.Account.LoginMethod", "Hoe wil je inloggen?", "How do you want to sign in?", "Jak chcesz się logować?", "Cum vrei să te autentifici?", "كيف تريد تسجيل الدخول؟");
        Add("Wa.Account.LoginMethodHint", "Microsoft of Google telt meteen als tweestapsverificatie. Met e-mail stel je die na afloop in.", "Microsoft or Google counts as two-factor right away. With e-mail you set it up afterwards.", "Microsoft lub Google liczy się od razu jako 2FA. Przy e-mailu ustawisz to później.", "Microsoft sau Google contează imediat ca 2FA. Cu e-mail o configurezi după.", "Microsoft أو Google يُحسبان فوراً كعاملَين. مع البريد تضبطه لاحقاً.");
        Add("Wa.Account.EmailPassword", "E-mail en wachtwoord", "E-mail and password", "E-mail i hasło", "E-mail și parolă", "البريد وكلمة المرور");
        Add("Wa.Account.Password", "Wachtwoord", "Password", "Hasło", "Parolă", "كلمة المرور");
        Add("Wa.Account.PasswordConfirm", "Bevestig wachtwoord", "Confirm password", "Potwierdź hasło", "Confirmă parola", "تأكيد كلمة المرور");
        Add("Wa.Account.PasswordMismatch", "Wachtwoorden komen niet overeen.", "Passwords do not match.", "Hasła nie są zgodne.", "Parolele nu coincid.", "كلمتا المرور غير متطابقتين.");
        Add("Wa.Account.SalesTitle", "Code van een salesmanager of partner", "Code from a sales manager or partner", "Kod od sales managera lub partnera", "Cod de la un sales manager sau partener", "رمز من مدير مبيعات أو شريك");
        Add("Wa.Account.SalesBody", "We onthouden de link waarmee je binnenkwam. Een code die je zelf typt, gaat voor.", "We remember the link you arrived with. A code you type yourself wins.", "Zapamiętujemy link wejścia. Kod wpisany ręcznie ma pierwszeństwo.", "Reținem linkul de intrare. Codul tastat are prioritate.", "نتذكر رابط الدخول. الرمز الذي تكتبه له الأولوية.");
        Add("Wa.Account.SalesViaLink", "via link", "from link", "przez link", "prin link", "عبر الرابط");
        Add("Wa.Account.SalesChange", "Wijzigen", "Change", "Zmień", "Schimbă", "تعديل");
        Add("Wa.Account.SalesCode", "Code van je accountmanager", "Code from your account manager", "Kod od opiekuna", "Cod de la account manager", "رمز مدير حسابك");
        Add("Wa.Account.SalesUnknown", "Deze code is onbekend. Je kunt wel doorgaan zonder code.", "This code is unknown. You can continue without a code.", "Ten kod jest nieznany. Możesz kontynuować bez kodu.", "Acest cod este necunoscut. Poți continua fără cod.", "هذا الرمز غير معروف. يمكنك المتابعة بدونه.");
        Add("Wa.Account.Terms", "Ik ga akkoord met de voorwaarden voor werkgevers en heb de privacyverklaring gelezen.", "I agree to the employer terms and have read the privacy statement.", "Akceptuję warunki dla pracodawców i przeczytałem oświadczenie o prywatności.", "Accept termenii pentru angajatori și am citit declarația de confidențialitate.", "أوافق على شروط أصحاب العمل وقرأت بيان الخصوصية.");
        Add("Wa.Account.Represent", "Ik mag {0} vertegenwoordigen op Lobsy.", "I may represent {0} on Lobsy.", "Mogę reprezentować {0} na Lobsy.", "Pot reprezenta {0} pe Lobsy.", "يحق لي تمثيل {0} على Lobsy.");
        Add("Wa.Account.ConsentRequired", "Bevestig de voorwaarden en dat je het bedrijf mag vertegenwoordigen.", "Confirm the terms and that you may represent the company.", "Potwierdź warunki i reprezentację firmy.", "Confirmă termenii și reprezentarea firmei.", "أكّد الشروط وأنك تمثّل الشركة.");
        Add("Wa.Account.Submit", "Account maken →", "Create account →", "Utwórz konto →", "Creează cont →", "إنشاء حساب →");
        Add("Wa.Account.PillMatch", "✓ Zakelijk", "✓ Business", "✓ Firmowy", "✓ Firmă", "✓ عمل");
        Add("Wa.Account.PillFreeMail", "Gratis mailadres", "Free mailbox", "Darmowa skrzynka", "Mail gratuit", "بريد مجاني");
        Add("Wa.Account.PillOther", "Ander domein", "Other domain", "Inna domena", "Alt domeniu", "نطاق آخر");
        Add("Wa.Account.DomainMatchHint", "Past bij {0}. Top: met dit adres verifieer je je bedrijf straks snel.", "Matches {0}. Great: you can verify your company quickly with this address.", "Pasuje do {0}. Świetnie: szybko zweryfikujesz firmę.", "Se potrivește cu {0}. Super: verifici rapid firma.", "يطابق {0}. رائع: تتحقق من شركتك بسرعة.");
        Add("Wa.Account.FreeMailHint", "Gratis mailadres: verificatie via brief.", "Free mailbox: verification by letter.", "Darmowa skrzynka: weryfikacja listem.", "Mail gratuit: verificare prin scrisoare.", "بريد مجاني: التحقق عبر خطاب.");

        // Code / done / link
        Add("Wa.Code.Eyebrow", "Bevestig je e-mail", "Confirm your e-mail", "Potwierdź e-mail", "Confirmă e-mailul", "أكّد بريدك");
        Add("Wa.Code.Title", "Vul de code in", "Enter the code", "Wpisz kod", "Introdu codul", "أدخل الرمز");
        Add("Wa.Code.Lead", "We stuurden een 6-cijferige code naar {0}.", "We sent a 6-digit code to {0}.", "Wysłaliśmy 6-cyfrowy kod na {0}.", "Am trimis un cod de 6 cifre la {0}.", "أرسلنا رمزاً من 6 أرقام إلى {0}.");
        Add("Wa.Code.Label", "Bevestigingscode", "Confirmation code", "Kod potwierdzenia", "Cod de confirmare", "رمز التأكيد");
        Add("Wa.Code.Countdown", "Geldig nog {0}:{1:D2}", "Valid for {0}:{1:D2}", "Ważny jeszcze {0}:{1:D2}", "Valabil încă {0}:{1:D2}", "صالح لمدة {0}:{1:D2}");
        Add("Wa.Code.Confirm", "Bevestigen", "Confirm", "Potwierdź", "Confirmă", "تأكيد");
        Add("Wa.Code.Resend", "Stuur nieuwe code", "Send a new code", "Wyślij nowy kod", "Trimite un cod nou", "أرسل رمزاً جديداً");
        Add("Wa.Code.Expired", "Code verlopen", "Code expired", "Kod wygasł", "Cod expirat", "انتهت صلاحية الرمز");
        Add("Wa.Code.ExpiredBody", "Vraag een nieuwe code aan. Je blijft op deze pagina.", "Request a new code. You stay on this page.", "Poproś o nowy kod. Pozostajesz na tej stronie.", "Solicită un cod nou. Rămâi pe această pagină.", "اطلب رمزاً جديداً. تبقى في هذه الصفحة.");

        Add("Wa.Done.Title", "Je account is klaar", "Your account is ready", "Twoje konto jest gotowe", "Contul tău este gata", "حسابك جاهز");
        Add("Wa.Done.Lead", "Welkom bij Lobsy. Je kunt verder met je dashboard.", "Welcome to Lobsy. Continue to your dashboard.", "Witamy w Lobsy. Przejdź do pulpitu.", "Bine ai venit pe Lobsy. Continuă la tabloul de bord.", "مرحباً بك في Lobsy. تابع إلى لوحة التحكم.");
        Add("Wa.Done.Verified", "Geverifieerd via je zakelijke e-mail", "Verified via your business e-mail", "Zweryfikowano przez firmowy e-mail", "Verificat prin e-mailul de firmă", "تم التحقق عبر بريد العمل");
        Add("Wa.Done.VerifyNext", "Volgende stap: verifieer je bedrijf zodat kandidaten je zien.", "Next: verify your company so candidates can see you.", "Dalej: zweryfikuj firmę, aby kandydaci Cię widzieli.", "Următorul pas: verifică firma ca să te vadă candidații.", "التالي: تحقّق من شركتك ليراك المرشحون.");
        Add("Wa.Done.Dashboard", "Naar dashboard", "Go to dashboard", "Do pulpitu", "La tabloul de bord", "إلى لوحة التحكم");
        Add("Wa.Done.Profile", "Over je bedrijf invullen", "Fill in about your company", "Uzupełnij dane firmy", "Completează despre firmă", "أكمل بيانات شركتك");

        Add("Wa.Link.Title", "Account koppelen", "Link account", "Połącz konto", "Conectează contul", "ربط الحساب");
        Add("Wa.Link.Lead", "We koppelen je Microsoft- of Google-login aan je nieuwe Lobsy-account.", "We link your Microsoft or Google login to your new Lobsy account.", "Łączymy logowanie Microsoft/Google z nowym kontem Lobsy.", "Conectăm autentificarea Microsoft/Google la noul cont Lobsy.", "نربط تسجيل Microsoft أو Google بحساب Lobsy الجديد.");
        Add("Wa.Link.Working", "Even geduld…", "One moment…", "Chwila…", "Un moment…", "لحظة…");
        Add("Wa.Link.EmailMismatch", "Dit account hoort bij een ander e-mailadres. Gebruik e-mail en wachtwoord of hetzelfde adres.", "This account belongs to another e-mail address. Use e-mail and password or the same address.", "To konto należy do innego e-maila. Użyj e-maila i hasła lub tego samego adresu.", "Acest cont aparține altui e-mail. Folosește e-mail și parolă sau aceeași adresă.", "هذا الحساب يخص بريداً آخر. استخدم البريد وكلمة المرور أو نفس العنوان.");

        // Step 5 — verification choice (WaVerify.*)
        Add("WaVerify.Eyebrow", "Stap 5 · Verifiëren", "Step 5 · Verify", "Krok 5 · Weryfikacja", "Pasul 5 · Verificare", "الخطوة 5 · التحقق");
        Add("WaVerify.Title", "Laat zien dat je bij {0} hoort", "Show that you belong to {0}", "Pokaż, że należysz do {0}", "Arată că aparții de {0}", "أظهر أنك تنتمي إلى {0}");
        Add("WaVerify.Lead", "Tot die tijd ben je onzichtbaar voor kandidaten. Kies hoe:", "Until then you are invisible to candidates. Choose how:", "Do tego czasu jesteś niewidoczny. Wybierz sposób:", "Până atunci ești invizibil. Alege cum:", "حتى ذلك الحين أنت غير مرئي. اختر كيف:");
        Add("WaVerify.Speech", "Laatste stap! Zo houden we Lobsy eerlijk.", "Last step! This keeps Lobsy honest.", "Ostatni krok! Tak Lobsy zostaje uczciwy.", "Ultimul pas! Așa Lobsy rămâne corect.", "الخطوة الأخيرة! هكذا تبقى Lobsy نزيهة.");
        Add("WaVerify.Email.Tag", "Snelst · ± 1 minuut", "Fastest · ± 1 minute", "Najszybciej · ± 1 min", "Cel mai rapid · ± 1 min", "الأسرع · ± دقيقة");
        Add("WaVerify.Email.Title", "Met je zakelijke e-mailadres", "With your business e-mail", "Firmowym e-mailem", "Cu e-mailul de firmă", "بعنوان بريد العمل");
        Add("WaVerify.Email.Body", "Het domein moet passen bij de website die bij KVK staat. We sturen een code naar dat adres.", "The domain must match the website at KVK. We send a code to that address.", "Domena musi pasować do strony w KVK. Wyślemy kod.", "Domeniul trebuie să se potrivească site-ului KVK. Trimitem un cod.", "يجب أن يطابق النطاق موقع KVK. نرسل رمزاً.");
        Add("WaVerify.Email.Website", "Website bij KVK: {0}", "Website at KVK: {0}", "Strona w KVK: {0}", "Site la KVK: {0}", "الموقع في KVK: {0}");
        Add("WaVerify.Email.Disabled", "Bij de KVK staat geen website. Kies de brief.", "No website at KVK. Choose the letter.", "Brak strony w KVK. Wybierz list.", "Niciun site la KVK. Alege scrisoarea.", "لا موقع في KVK. اختر الخطاب.");
        Add("WaVerify.Email.FreeMail", "Gmail, Outlook.com, Hotmail en andere gratis adressen tellen niet mee.", "Gmail, Outlook.com, Hotmail and other free addresses do not count.", "Gmail, Outlook.com, Hotmail i inne darmowe nie liczą się.", "Gmail, Outlook.com, Hotmail și altele gratuite nu contează.", "Gmail وOutlook وHotmail وغيرها المجانية لا تُحسب.");
        Add("WaVerify.Email.Step1", "We mailen een code van 6 cijfers", "We e-mail a 6-digit code", "Wysyłamy 6-cyfrowy kod", "Trimitem un cod de 6 cifre", "نرسل رمزاً من 6 أرقام");
        Add("WaVerify.Email.Step2", "Vul de code in (10 minuten geldig)", "Enter the code (valid 10 minutes)", "Wpisz kod (ważny 10 min)", "Introdu codul (valabil 10 min)", "أدخل الرمز (صالح 10 دقائق)");
        Add("WaVerify.Email.Step3", "Klaar: je bedrijf is meteen zichtbaar", "Done: your company is immediately visible", "Gotowe: firma od razu widoczna", "Gata: firma e imediat vizibilă", "تم: شركتك تظهر فوراً");
        Add("WaVerify.Email.Cta", "Stuur de code naar {0} →", "Send the code to {0} →", "Wyślij kod na {0} →", "Trimite codul la {0} →", "أرسل الرمز إلى {0} ←");
        Add("WaVerify.Email.Label", "Zakelijk e-mailadres", "Business e-mail", "Firmowy e-mail", "E-mail de firmă", "بريد العمل");
        Add("WaVerify.Email.CodeLabel", "Code uit de mail", "Code from e-mail", "Kod z e-maila", "Cod din e-mail", "الرمز من البريد");
        Add("WaVerify.Email.Confirm", "Bedrijf verifiëren →", "Verify company →", "Zweryfikuj firmę →", "Verifică firma →", "تحقّق من الشركة ←");
        Add("WaVerify.Letter.Tag", "1 tot 3 werkdagen", "1 to 3 business days", "1–3 dni robocze", "1–3 zile lucrătoare", "1–3 أيام عمل");
        Add("WaVerify.Letter.Title", "Met een brief op het KvK-adres", "With a letter to the KvK address", "Listem na adres KVK", "Cu o scrisoare la adresa KVK", "بخطاب إلى عنوان KVK");
        Add("WaVerify.Letter.Body", "Geen zakelijk adres, of past je domein niet? We sturen een brief met een code naar het adres dat bij KVK staat.", "No business address, or domain mismatch? We send a letter with a code to the KvK address.", "Brak firmowego adresu? Wyślemy list z kodem na adres KVK.", "Fără e-mail de firmă? Trimitem o scrisoare cu cod la adresa KVK.", "بدون بريد عمل؟ نرسل خطاباً برمز إلى عنوان KVK.");
        Add("WaVerify.Letter.Step1", "Wij versturen de brief via PostNL", "We send the letter via PostNL", "Wysyłamy list przez PostNL", "Trimitem scrisoarea prin PostNL", "نرسل الخطاب عبر PostNL");
        Add("WaVerify.Letter.Step2", "Vul de code in via je dashboard", "Enter the code in your dashboard", "Wpisz kod w panelu", "Introdu codul în tablou", "أدخل الرمز في لوحة التحكم");
        Add("WaVerify.Letter.Step3", "De code is 30 dagen geldig", "The code is valid for 30 days", "Kod ważny 30 dni", "Codul e valabil 30 zile", "الرمز صالح 30 يوماً");
        Add("WaVerify.Letter.AddressHint", "Staat het adres niet goed bij KVK? Pas het eerst aan bij KVK.", "Wrong address at KVK? Update it at KVK first.", "Zły adres w KVK? Najpierw popraw w KVK.", "Adres greșit la KVK? Corectează mai întâi la KVK.", "عنوان خاطئ في KVK؟ عدّله أولاً في KVK.");
        Add("WaVerify.Letter.Cta", "Stuur de brief →", "Send the letter →", "Wyślij list →", "Trimite scrisoarea →", "أرسل الخطاب ←");
        Add("WaVerify.Letter.Cap", "Tijdelijk niet beschikbaar, vraag een handmatige controle aan.", "Temporarily unavailable — request a manual check.", "Tymczasowo niedostępne — poproś o kontrolę ręczną.", "Temporar indisponibil — cere o verificare manuală.", "غير متاح مؤقتاً — اطلب فحصاً يدوياً.");
        Add("WaVerify.Manual.Lead", "Lukt geen van beide?", "Neither option works?", "Żadna opcja nie działa?", "Niciuna nu merge?", "لا يعمل أي خيار؟");
        Add("WaVerify.Manual.Cta", "Handmatige controle aanvragen", "Request a manual check", "Poproś o kontrolę ręczną", "Cere verificare manuală", "اطلب فحصاً يدوياً");
        Add("WaVerify.Manual.Hint", "We reageren binnen 2 werkdagen.", "We reply within 2 business days.", "Odpowiadamy w 2 dni robocze.", "Răspundem în 2 zile lucrătoare.", "نرد خلال يومي عمل.");
        Add("WaVerify.Manual.Reason", "Waarom lukt het niet?", "Why doesn't it work?", "Dlaczego nie działa?", "De ce nu merge?", "لماذا لا يعمل؟");
        Add("WaVerify.Manual.Message", "Toelichting (optioneel)", "Details (optional)", "Szczegóły (opcjonalnie)", "Detalii (opțional)", "تفاصيل (اختياري)");
        Add("WaVerify.Manual.Submit", "Verzoek indienen", "Submit request", "Wyślij wniosek", "Trimite cererea", "إرسال الطلب");
        Add("WaVerify.Manual.Pending", "Je handmatige controle staat open. We reageren binnen 2 werkdagen.", "Your manual check is open. We reply within 2 business days.", "Kontrola ręczna jest otwarta. Odpowiedź w 2 dni robocze.", "Verificarea manuală e deschisă. Răspuns în 2 zile lucrătoare.", "الفحص اليدوي مفتوح. نرد خلال يومي عمل.");
        Add("WaVerify.Allowed.Title", "Wat kan al, en wat na verificatie?", "What works now, and after verification?", "Co już możesz, a co po weryfikacji?", "Ce poți acum și după verificare?", "ما المتاح الآن وبعد التحقق؟");
        Add("WaVerify.Allowed.1", "Vacatures als concept klaarzetten", "Prepare vacancy drafts", "Przygotuj szkice ofert", "Pregătește drafturi", "جهّز مسودات الوظائف");
        Add("WaVerify.Allowed.2", "Collega’s uitnodigen", "Invite colleagues", "Zaproś kolegów", "Invită colegi", "ادعُ الزملاء");
        Add("WaVerify.Allowed.3", "Profiel, cultuur en branche invullen", "Fill profile, culture and industry", "Uzupełnij profil, kulturę i branżę", "Completează profil, cultură și industrie", "أكمل الملف والثقافة والقطاع");
        Add("WaVerify.Blocked.1", "Kandidaten en sollicitaties zien", "See candidates and applications", "Zobacz kandydatów i aplikacje", "Vezi candidați și aplicații", "رؤية المرشحين والطلبات");
        Add("WaVerify.Blocked.2", "Zichtbaar zijn voor kandidaten", "Be visible to candidates", "Być widocznym dla kandydatów", "Fi vizibil pentru candidați", "الظهور للمرشحين");
        Add("WaVerify.Blocked.3", "Tokens kopen", "Buy tokens", "Kup tokeny", "Cumpără tokeni", "شراء الرموز");
        Add("WaVerify.Allowed.Note", "Conceptvacatures gaan automatisch live zodra je geverifieerd bent.", "Draft vacancies go live automatically once verified.", "Szkice ofert publikują się automatycznie po weryfikacji.", "Drafturile apar automat după verificare.", "المسودات تُنشر تلقائياً بعد التحقق.");
        Add("WaVerify.Already", "Je bedrijf is al geverifieerd.", "Your company is already verified.", "Firma jest już zweryfikowana.", "Firma este deja verificată.", "شركتك موثّقة بالفعل.");
        Add("WaVerify.Dashboard", "Naar mijn dashboard →", "To my dashboard →", "Do pulpitu →", "La tabloul de bord →", "إلى لوحة التحكم ←");

        // Letter on its way (wr-d9 / wr-m7)
        Add("WaVerify.Brief.Badge", "Brief onderweg", "Letter on its way", "List w drodze", "Scrisoare pe drum", "الخطاب في الطريق");
        Add("WaVerify.Brief.Title", "Je brief is onderweg", "Your letter is on its way", "Twój list jest w drodze", "Scrisoarea ta e pe drum", "خطابك في الطريق");
        Add("WaVerify.Brief.Lead", "We stuurden een brief naar het KvK-adres van {0}. Vul de code in zodra je hem hebt.", "We sent a letter to the KvK address of {0}. Enter the code when you have it.", "Wysłaliśmy list na adres KVK {0}. Wpisz kod gdy go masz.", "Am trimis o scrisoare la adresa KVK a {0}. Introdu codul când îl ai.", "أرسلنا خطاباً إلى عنوان KVK لـ {0}. أدخل الرمز عند استلامه.");
        Add("WaVerify.Brief.CodeTitle", "Vul je code in", "Enter your code", "Wpisz kod", "Introdu codul", "أدخل الرمز");
        Add("WaVerify.Brief.CodeHint", "8 tekens, staat onder ‘Uw verificatiecode’. Hoofdletters maken niet uit.", "8 characters, under ‘Your verification code’. Case does not matter.", "8 znaków pod ‘Uw verificatiecode’. Wielkość liter bez znaczenia.", "8 caractere sub ‘Uw verificatiecode’. Majusculele nu contează.", "8 أحرف تحت ‘Uw verificatiecode’. حالة الأحرف لا تهم.");
        Add("WaVerify.Brief.Confirm", "Bedrijf verifiëren →", "Verify company →", "Zweryfikuj firmę →", "Verifică firma →", "تحقّق من الشركة ←");
        Add("WaVerify.Brief.Attempts", "Na 5 foute pogingen blokkeren we de code en vraagt support ernaar te kijken.", "After 5 wrong attempts we block the code and ask support to look into it.", "Po 5 błędach blokujemy kod i prosimy support.", "După 5 încercări greșite blocăm codul.", "بعد 5 محاولات خاطئة نوقف الرمز.");
        Add("WaVerify.Brief.Resend", "Niet ontvangen? Opnieuw versturen kan vanaf {0} (nog {1} keer).", "Not received? Resend possible from {0} ({1} left).", "Nie doszło? Ponowne wysłanie od {0} (jeszcze {1}).", "Nu a ajuns? Retrimitere din {0} (încă {1}).", "لم يصل؟ إعادة الإرسال من {0} (متبقي {1}).");
        Add("WaVerify.Brief.ResendCta", "Brief opnieuw versturen", "Resend letter", "Wyślij list ponownie", "Retrimite scrisoarea", "أعد إرسال الخطاب");
        Add("WaVerify.Brief.EmailAlt", "Toch sneller? Verifieer met e-mail", "Faster? Verify by e-mail", "Szybciej? Zweryfikuj e-mailem", "Mai rapid? Verifică pe e-mail", "أسرع؟ تحقّق بالبريد");
        Add("WaVerify.Brief.Later", "Je kunt de code ook later in je dashboard invullen.", "You can also enter the code later in your dashboard.", "Kod możesz też wpisać później w panelu.", "Poți introduce codul mai târziu în tablou.", "يمكنك إدخال الرمز لاحقاً في لوحة التحكم.");
        Add("WaVerify.Brief.Delivery", "Verwacht {0} op {1}. Code geldig t/m {2}.", "Expect {0} at {1}. Code valid through {2}.", "Oczekuj {0} pod {1}. Kod ważny do {2}.", "Așteaptă {0} la {1}. Cod valabil până la {2}.", "توقّع {0} على {1}. الرمز صالح حتى {2}.");
        Add("WaVerify.Brief.Speech", "De postbode komt eraan!", "The postman is on the way!", "Listonosz już jedzie!", "Poștașul e pe drum!", "ساعي البريد في الطريق!");

        Add("AdminWa.Title", "Werkgeververificatie", "Employer verification", "Weryfikacja pracodawców", "Verificare angajatori", "تحقق أصحاب العمل");
        Add("AdminWa.Lead", "Handmatige controles, gemarkeerde registraties en geblokkeerde brieven.", "Manual checks, flagged registrations and blocked letters.", "Kontrole ręczne, oznaczone rejestracje i zablokowane listy.", "Verificări manuale, înregistrări semnalate și scrisori blocate.", "فحوصات يدوية وتسجيلات معلّمة وخطابات موقوفة.");
        Add("AdminWa.Tab.Manual", "Handmatig", "Manual", "Ręcznie", "Manual", "يدوي");
        Add("AdminWa.Tab.Flagged", "Gemarkeerd", "Flagged", "Oznaczone", "Semnalate", "معلّم");
        Add("AdminWa.Tab.Letters", "Brieven", "Letters", "Listy", "Scrisori", "خطابات");
        Add("AdminWa.Tab.Access", "Toegang", "Access", "Dostęp", "Acces", "الوصول");
        Add("AdminWa.Approve", "Goedkeuren", "Approve", "Zatwierdź", "Aprobă", "موافقة");
        Add("AdminWa.Reject", "Afwijzen", "Reject", "Odrzuć", "Respinge", "رفض");
        Add("AdminWa.SendLetter", "Brief sturen", "Send letter", "Wyślij list", "Trimite scrisoare", "إرسال خطاب");
        Add("AdminWa.StubPdf", "Bekijk testbrief", "View test letter", "Zobacz list testowy", "Vezi scrisoarea test", "عرض خطاب الاختبار");
        Add("AdminWa.RejectReason", "Reden (verplicht)", "Reason (required)", "Powód (wymagany)", "Motiv (obligatoriu)", "السبب (مطلوب)");
        Add("AdminWa.Access.Managers", "Beheerders (alleen admin)", "Managers (admin only)", "Managerowie (tylko admin)", "Manageri (doar admin)", "المديرون (للمشرف فقط)");
        Add("AdminWa.Access.Ownership", "Eigendomsoverdracht", "Ownership transfer", "Przeniesienie własności", "Transfer proprietate", "نقل الملكية");
        Add("AdminWa.Access.LetterOk", "Brief bevestigd", "Letter confirmed", "List potwierdzony", "Scrisoare confirmată", "تم تأكيد الخطاب");
        Add("AdminWa.Access.LetterPending", "Brief nog open", "Letter pending", "List oczekuje", "Scrisoare în așteptare", "الخطاب معلّق");
        Add("Nav.WerkgeverVerificatie", "Werkgeververificatie", "Employer verification", "Weryfikacja pracodawców", "Verificare angajatori", "تحقق أصحاب العمل");

        // Access request (07)
        Add("WaAccess.Speech", "Geen zorgen! Je collega's zijn je voor geweest. Vraag ze om toegang, dan werk je samen in één account.", "No worries! Your colleagues got here first. Ask them for access so you work in one account.", "Spokojnie! Koledzy byli wcześniej. Poproś o dostęp i pracujcie w jednym koncie.", "Nicio grijă! Colegii au fost înainte. Cere acces ca să lucrați într-un singur cont.", "لا تقلق! زملاؤك سبقوك. اطلب الوصول لتعملوا في حساب واحد.");
        Add("WaAccess.Title", "Dit bedrijf staat al op Lobsy", "This company is already on Lobsy", "Ta firma jest już na Lobsy", "Această firmă e deja pe Lobsy", "هذه الشركة موجودة بالفعل على Lobsy");
        Add("WaAccess.Lead", "Er is al een bedrijfsmanager. Jij kunt geen tweede eigenaar worden, maar wel toegang aanvragen.", "There is already a company manager. You cannot become a second owner, but you can request access.", "Jest już menedżer firmy. Nie możesz zostać drugim właścicielem, ale możesz poprosić o dostęp.", "Există deja un manager. Nu poți fi al doilea proprietar, dar poți cere acces.", "يوجد مدير شركة بالفعل. لا يمكنك أن تصبح مالكاً ثانياً، لكن يمكنك طلب الوصول.");
        Add("WaAccess.Privacy", "We sturen je verzoek naar de huidige beheerder. Wie dat is, laten we om privacyredenen niet zien.", "We send your request to the current manager. For privacy we do not show who that is.", "Wysyłamy prośbę do obecnego managera. Ze względów prywatności nie pokazujemy kim jest.", "Trimitem cererea managerului actual. Din motive de confidențialitate nu arătăm cine este.", "نرسل طلبك إلى المدير الحالي. لأسباب الخصوصية لا نُظهر من هو.");
        Add("WaAccess.FormTitle", "Vraag toegang aan", "Request access", "Poproś o dostęp", "Cere acces", "اطلب الوصول");
        Add("WaAccess.Name", "Je naam", "Your name", "Twoje imię", "Numele tău", "اسمك");
        Add("WaAccess.Function", "Functie", "Job title", "Stanowisko", "Funcție", "المسمى الوظيفي");
        Add("WaAccess.Email", "Je zakelijke e-mailadres", "Your work e-mail", "Twój służbowy e-mail", "E-mailul de serviciu", "بريد العمل");
        Add("WaAccess.Phone", "Telefoon (optioneel)", "Phone (optional)", "Telefon (opcjonalnie)", "Telefon (opțional)", "الهاتف (اختياري)");
        Add("WaAccess.Role", "Welke rol zoek je?", "Which role do you need?", "Jakiej roli potrzebujesz?", "Ce rol cauți?", "ما الدور الذي تحتاجه؟");
        Add("WaAccess.Role.Branch", "Vestigingsmanager", "Branch manager", "Kierownik placówki", "Manager locație", "مدير فرع");
        Add("WaAccess.Role.Regional", "Regiomanager", "Regional manager", "Kierownik regionu", "Manager regional", "مدير إقليمي");
        Add("WaAccess.Role.Enterprise", "Bedrijfsmanager", "Company manager", "Menedżer firmy", "Manager firmă", "مدير الشركة");
        Add("WaAccess.Message", "Bericht (optioneel)", "Message (optional)", "Wiadomość (opcjonalnie)", "Mesaj (opțional)", "رسالة (اختياري)");
        Add("WaAccess.Submit", "Verzoek versturen", "Send request", "Wyślij prośbę", "Trimite cererea", "إرسال الطلب");
        Add("WaAccess.CodeTitle", "Bevestig je e-mail", "Confirm your e-mail", "Potwierdź e-mail", "Confirmă e-mailul", "أكّد بريدك");
        Add("WaAccess.CodeConfirm", "Bevestigen", "Confirm", "Potwierdź", "Confirmă", "تأكيد");
        Add("WaAccess.Sent", "Je aanvraag is verstuurd.", "Your request was sent.", "Twoja prośba została wysłana.", "Cererea a fost trimisă.", "تم إرسال طلبك.");
        Add("WaAccess.Timeline.Title", "Wat gebeurt er dan?", "What happens next?", "Co dalej?", "Ce urmează?", "ماذا يحدث بعد ذلك؟");
        Add("WaAccess.Timeline.1", "De bedrijfsmanager krijgt je verzoek (vandaag)", "The company manager receives your request (today)", "Menedżer dostaje prośbę (dziś)", "Managerul primește cererea (azi)", "يستلم مدير الشركة طلبك (اليوم)");
        Add("WaAccess.Timeline.2", "Na 3 werkdagen een herinnering", "A reminder after 3 working days", "Przypomnienie po 3 dniach roboczych", "Reminder după 3 zile lucrătoare", "تذكير بعد 3 أيام عمل");
        Add("WaAccess.Timeline.3", "Geen reactie? Dan kijkt Lobsy-support mee (dag 5)", "No reply? Lobsy support steps in (day 5)", "Brak odpowiedzi? Support Lobsy (dzień 5)", "Fără răspuns? Support Lobsy (ziua 5)", "لا رد؟ تدخل دعم Lobsy (اليوم 5)");
        Add("WaAccess.Ownership.Title", "Eigendom overnemen", "Take over ownership", "Przejmij własność", "Preia proprietatea", "تولَّ الملكية");
        Add("WaAccess.Ownership.Body", "Is de beheerder vertrokken en ben jij nu verantwoordelijk? Dan kun je het eigendom overnemen. We sturen een brief met code naar het KvK-adres; support controleert het verzoek.", "Has the manager left and are you responsible now? You can take over ownership. We send a letter with a code to the KvK address; support reviews the request.", "Manager odszedł i ty odpowiadasz? Możesz przejąć własność. Wyślemy list z kodem na adres KvK; support sprawdzi.", "Managerul a plecat și tu ești responsabil? Poți prelua proprietatea. Trimitem o scrisoare cu cod la adresa KvK; support verifică.", "هل غادر المدير وأنت المسؤول الآن؟ يمكنك تولّي الملكية. نرسل خطاباً برمز إلى عنوان KvK؛ يراجع الدعم الطلب.");
        Add("WaAccess.Ownership.Cta", "Eigendom overnemen", "Take over ownership", "Przejmij własność", "Preia proprietatea", "تولَّ الملكية");
        Add("WaAccess.OtherCompany", "Ander bedrijf", "Other company", "Inna firma", "Altă firmă", "شركة أخرى");
        Add("WaAccess.ManagedBadge", "Deze vestiging wordt al beheerd op Lobsy", "This location is already managed on Lobsy", "Ta lokalizacja jest już zarządzana na Lobsy", "Această locație e deja gestionată pe Lobsy", "هذا الموقع يُدار بالفعل على Lobsy");
        Add("Employer.AccessRequests", "Toegangsverzoeken", "Access requests", "Prośby o dostęp", "Cereri de acces", "طلبات الوصول");
        Add("Employer.TakeoversSection", "Overname", "Takeovers", "Przejęcia", "Preluări", "عمليات الاستحواذ");
        Add("Employer.Access.Grant", "Toegang geven", "Grant access", "Przyznaj dostęp", "Acordă acces", "منح الوصول");
        Add("Employer.Access.Reject", "Afwijzen", "Reject", "Odrzuć", "Respinge", "رفض");
        Add("Employer.Access.Empty", "Geen openstaande toegangsverzoeken.", "No open access requests.", "Brak otwartych próśb o dostęp.", "Nicio cerere de acces deschisă.", "لا توجد طلبات وصول مفتوحة.");

        // Step 4 — Over je bedrijf (WaProfile.*)
        Add("WaProfile.Eyebrow.Branche", "Stap 4 · Over je bedrijf · 1 van 3", "Step 4 · About your company · 1 of 3", "Krok 4 · O firmie · 1 z 3", "Pasul 4 · Despre firmă · 1 din 3", "الخطوة 4 · عن شركتك · 1 من 3");
        Add("WaProfile.Eyebrow.Cultuur", "Stap 4 · Over je bedrijf · 2 van 3", "Step 4 · About your company · 2 of 3", "Krok 4 · O firmie · 2 z 3", "Pasul 4 · Despre firmă · 2 din 3", "الخطوة 4 · عن شركتك · 2 من 3");
        Add("WaProfile.Eyebrow.Betrokkenheid", "Stap 4 · Over je bedrijf · 3 van 3", "Step 4 · About your company · 3 of 3", "Krok 4 · O firmie · 3 z 3", "Pasul 4 · Despre firmă · 3 din 3", "الخطوة 4 · عن شركتك · 3 من 3");
        Add("WaProfile.Optional", "optioneel", "optional", "opcjonalne", "opțional", "اختياري");
        Add("WaProfile.OptionalMinute", "optioneel · 1 minuut", "optional · 1 minute", "opcjonalne · 1 min", "opțional · 1 min", "اختياري · دقيقة");
        Add("WaProfile.Mini.Branche", "Branche", "Industry", "Branża", "Industrie", "القطاع");
        Add("WaProfile.Mini.Cultuur", "Zo werken wij", "How we work", "Jak pracujemy", "Cum lucrăm", "كيف نعمل");
        Add("WaProfile.Mini.Betrokkenheid", "Betrokkenheid", "Engagement", "Zaangażowanie", "Implicare", "المشاركة");
        Add("WaProfile.Mini.Hint", "≈ 3 minuten, alles optioneel", "≈ 3 minutes, all optional", "≈ 3 min, wszystko opcjonalne", "≈ 3 min, tot opțional", "≈ 3 دقائق، كله اختياري");
        Add("WaProfile.Skip", "Later invullen", "Fill in later", "Uzupełnij później", "Completează mai târziu", "أكمل لاحقاً");
        Add("WaProfile.Next", "Verder →", "Next →", "Dalej →", "Mai departe →", "التالي →");
        Add("WaProfile.SaveNext", "Opslaan en verder", "Save and continue", "Zapisz i dalej", "Salvează și continuă", "احفظ وتابع");
        Add("WaProfile.Speech.Branche", "Wat doen jullie? Dit heb ik bij KVK gevonden. Klopt het? Tik gerust meer aan.", "What do you do? I found this at KVK. Does it look right? Tap more if you like.", "Czym się zajmujecie? Znalazłem to w KVK. Pasuje? Dodaj więcej.", "Ce faceți? Am găsit asta la KVK. E corect? Adaugă mai multe.", "ماذا تفعلون؟ وجدت هذا في KVK. هل صحيح؟ اختر المزيد.");
        Add("WaProfile.Speech.Cultuur", "Kandidaten doen dezelfde test. Hoe eerlijker jij kiest, hoe beter de match.", "Candidates take the same test. The more honest you are, the better the match.", "Kandydaci robią ten sam test. Im szczerzej, tym lepszy match.", "Candidații fac același test. Cu cât ești mai sincer, cu atât match-ul e mai bun.", "المرشحون يؤدون نفس الاختبار. كلّما كنت أصدق تحسّن التطابق.");
        Add("WaProfile.Speech.Betrokkenheid", "Maak niets mooier dan het is. Wat wij niet kunnen controleren, noemen we ‘door werkgever opgegeven’.", "Don't overstate. What we can't verify, we label ‘provided by employer’.", "Nie upiększaj. Czego nie sprawdzimy, oznaczymy ‘podane przez pracodawcę’.", "Nu înfrumuseța. Ce nu verificăm e ‘declarat de angajator’.", "لا تبالغ. ما لا نتحقق منه نسميه ‘مقدّم من صاحب العمل’.");

        Add("WaProfile.Branche.Title", "In welke branche werken jullie?", "Which industries do you work in?", "W jakiej branży pracujecie?", "În ce industrie lucrați?", "في أي قطاع تعملون؟");
        Add("WaProfile.Branche.Lead", "Kies alles wat past. We hebben alvast ingevuld wat bij KVK staat. Kandidaten vinden je zo op de banenkaart en in Match.", "Choose everything that fits. We prefilled what KVK lists. Candidates find you on the job map and in Match.", "Wybierz co pasuje. Wypełniliśmy dane z KVK. Kandydaci znajdą cię na mapie i w Match.", "Alege ce se potrivește. Am completat din KVK. Candidații te găsesc pe hartă și în Match.", "اختر كل ما يناسب. عبّأنا ما لدى KVK. يجدك المرشحون على الخريطة وفي Match.");
        Add("WaProfile.Branche.FromKvk", "Uit KvK", "From KvK", "Z KvK", "Din KvK", "من KvK");
        Add("WaProfile.Branche.Suggested", "voorgesteld op basis van je KvK-inschrijving", "suggested from your KvK registration", "zaproponowane z rejestracji KvK", "sugerat din înregistrarea KvK", "مقترح من تسجيل KvK");
        Add("WaProfile.Branche.Count", "{0} gekozen · max. {1}", "{0} selected · max. {1}", "{0} wybrano · max. {1}", "{0} alese · max. {1}", "{0} مختارة · حد أقصى {1}");
        Add("WaProfile.Branche.HintTitle", "Per vacature kies je er straks 1 of 2", "Per vacancy you will choose 1 or 2 later", "Przy ofercie wybierzesz 1 lub 2", "La fiecare ofertă alegi 1 sau 2", "لكل وظيفة تختار 1 أو 2 لاحقاً");
        Add("WaProfile.Branche.HintBody", "Het bedrijf mag meerdere branches hebben. Een vacature krijgt automatisch de eerste; je kunt dat per vacature aanpassen.", "The company may have several industries. A vacancy gets the first one automatically; you can change that per vacancy.", "Firma może mieć kilka branż. Oferta dostaje pierwszą automatycznie; możesz zmienić.", "Firma poate avea mai multe industrii. Oferta primește prima automat; poți schimba.", "يمكن للشركة أن تملك عدة قطاعات. تحصل الوظيفة تلقائياً على الأول؛ يمكنك التعديل.");
        Add("WaProfile.Branche.SbiLine", "Uit het Handelsregister:", "From the Trade Register:", "Z rejestru:", "Din registru:", "من السجل التجاري:");

        Add("WaProfile.Cultuur.Title", "Zo werken wij", "How we work", "Jak pracujemy", "Cum lucrăm", "كيف نعمل");
        Add("WaProfile.Cultuur.Lead", "Zet de bolletjes waar jullie écht staan, niet waar je graag zou willen staan. Kandidaten met dezelfde voorkeuren komen dan hoger in je lijst.", "Set the dots where you really are, not where you'd like to be. Candidates with the same preferences rank higher.", "Ustaw kropki tam, gdzie naprawdę jesteście. Kandydaci z tymi samymi preferencjami będą wyżej.", "Pune punctele unde sunteți cu adevărat. Candidații cu aceleași preferințe urcă.", "ضع النقاط حيث أنتم فعلاً، لا حيث تودّون. المرشحون بنفس التفضيلات يرتفعون.");
        Add("WaProfile.Cultuur.SlidersTitle", "Hoe gaat het bij jullie?", "How does it work at yours?", "Jak u was wygląda?", "Cum e la voi?", "كيف يسير الأمر لديكم؟");
        Add("WaProfile.Cultuur.CardsTitle", "Kies 3 kernwaarden", "Choose 3 core values", "Wybierz 3 wartości", "Alege 3 valori", "اختر 3 قيم أساسية");
        Add("WaProfile.Cultuur.CardsLead", "Wat vinden jullie het belangrijkst? Elke kaart hoort bij een van de 5 werkwaarden uit de Waardentest.", "What matters most? Each card maps to one of the 5 workplace values from the Values test.", "Co jest najważniejsze? Każda karta należy do 5 wartości z testu.", "Ce contează cel mai mult? Fiecare card e una din 5 valori.", "ما الأهم لديكم؟ كل بطاقة ترتبط بإحدى قيم العمل الخمس.");
        Add("WaProfile.Cultuur.CardsCount", "{0} van {1}", "{0} of {1}", "{0} z {1}", "{0} din {1}", "{0} من {1}");
        Add("WaProfile.Cultuur.ProfileTitle", "Jullie cultuurprofiel", "Your culture profile", "Wasz profil kultury", "Profilul vostru cultural", "ملف ثقافتكم");
        Add("WaProfile.Cultuur.ProfileLead", "Dezelfde 6 dimensies als de Cultuurscan van kandidaten, plus je top-3 waarden.", "The same 6 dimensions as the candidate Culture scan, plus your top-3 values.", "Te same 6 wymiary co skan kultury, plus top-3 wartości.", "Aceleași 6 dimensiuni ca scanarea culturală, plus top-3.", "نفس الأبعاد الستة لاختبار الثقافة، إضافة إلى أهم 3 قيم.");
        Add("WaProfile.Cultuur.FullHint", "De volledige Cultuurscan (12 vragen) staat later in je dashboard.", "The full Culture scan (12 questions) is later in your dashboard.", "Pełny skan kultury (12 pytań) będzie w panelu.", "Scanarea completă (12 întrebări) e în tablou.", "اختبار الثقافة الكامل (12 سؤالاً) لاحقاً في لوحتك.");
        Add("WaProfile.Cultuur.Live", "live", "Live", "na żywo", "în direct", "مباشر");
        Add("WaProfile.Cultuur.QuickBanner", "Ingevuld via snelle schuifjes, verfijn met de volledige scan", "Filled via quick sliders — refine with the full scan", "Wypełnione suwakami — doprecyzuj pełnym skanem", "Completat cu glisoare — rafinează cu scanarea completă", "عُبئ عبر المنزلقات السريعة — حسّنه بالاختبار الكامل");
        Add("WaProfile.Cultuur.ShowMore", "+ 2 vragen", "+ 2 questions", "+ 2 pytania", "+ 2 întrebări", "+ سؤالان");

        Add("WaProfile.Slider.Autonomy.Low", "Duidelijke kaders", "Clear frameworks", "Jasne ramy", "Cadre clare", "أطر واضحة");
        Add("WaProfile.Slider.Autonomy.High", "Zelfstandig werken", "Working independently", "Samodzielna praca", "Lucru independent", "عمل مستقل");
        Add("WaProfile.Slider.Informal.Low", "Formeel", "Formal", "Formalnie", "Formal", "رسمي");
        Add("WaProfile.Slider.Informal.High", "Informeel", "Informal", "Nieformalnie", "Informal", "غير رسمي");
        Add("WaProfile.Slider.Collaboration.Low", "Ieder z’n eigen taken", "Everyone their own tasks", "Każdy swoje zadania", "Fiecare cu sarcinile lui", "كلٌّ بمهامه");
        Add("WaProfile.Slider.Collaboration.High", "Veel samenwerken", "Lots of collaboration", "Dużo współpracy", "Multă colaborare", "تعاون كثير");
        Add("WaProfile.Slider.Flexibility.Low", "Vaste structuur", "Fixed structure", "Stała struktura", "Structură fixă", "هيكل ثابت");
        Add("WaProfile.Slider.Flexibility.High", "Flexibel meebewegen", "Moving flexibly", "Elastyczne dostosowanie", "Flexibilitate", "مرونة في التكيّف");
        Add("WaProfile.Slider.Innovation.Low", "Beproefd en stabiel", "Proven and stable", "Sprawdzone i stabilne", "Probat și stabil", "مجرّب ومستقر");
        Add("WaProfile.Slider.Innovation.High", "Nieuwe dingen proberen", "Trying new things", "Próbowanie nowego", "Încercăm lucruri noi", "تجربة أشياء جديدة");
        Add("WaProfile.Slider.PeopleFirst.Low", "Resultaat voorop", "Results first", "Wynik na pierwszym miejscu", "Rezultatul întâi", "النتيجة أولاً");
        Add("WaProfile.Slider.PeopleFirst.High", "Mensen voorop", "People first", "Ludzie na pierwszym miejscu", "Oamenii întâi", "الناس أولاً");

        Add("WaProfile.Card.vrijheid", "Vrijheid in hoe je je werk doet", "Freedom in how you do your work", "Wolność w sposobie pracy", "Libertate în cum lucrezi", "حرية في كيفية أداء عملك");
        Add("WaProfile.Card.uitdaging", "Elke dag iets nieuws leren", "Learn something new every day", "Codziennie uczyć się nowego", "Înveți ceva nou zilnic", "تعلّم شيئاً جديداً كل يوم");
        Add("WaProfile.Card.teamgevoel", "Een hecht team", "A close-knit team", "Zgrany zespół", "O echipă unită", "فريق متماسك");
        Add("WaProfile.Card.zorg", "We zorgen voor elkaar en onze klanten", "We care for each other and our customers", "Dbamy o siebie i klientów", "Avem grijă unii de alții și de clienți", "نعتني ببعضنا وبعملائنا");
        Add("WaProfile.Card.groei", "Doorgroeien en beter worden", "Grow and get better", "Rozwijać się i być lepszym", "Crești și te îmbunătățești", "التطوّر والتحسّن");
        Add("WaProfile.Card.resultaat", "Samen resultaat halen", "Achieve results together", "Wspólnie osiągać wyniki", "Obținem rezultate împreună", "تحقيق النتائج معاً");
        Add("WaProfile.Card.zekerheid", "Vaste afspraken en zekerheid", "Clear agreements and security", "Stałe ustalenia i pewność", "Acorduri clare și siguranță", "اتفاقيات ثابتة وأمان");
        Add("WaProfile.Card.vakmanschap", "Degelijk vakmanschap", "Solid craftsmanship", "Solidne rzemiosło", "Meșteșug solid", "حرفية متينة");
        Add("WaProfile.Card.betekenis", "Werk dat ertoe doet", "Work that matters", "Praca, która ma znaczenie", "Muncă ce contează", "عمل له معنى");
        Add("WaProfile.Card.eerlijk", "Eerlijk en duurzaam ondernemen", "Fair and sustainable business", "Uczciwy i zrównoważony biznes", "Afaceri corecte și durabile", "أعمال عادلة ومستدامة");

        Add("WaProfile.Engage.Coming", "Volgt", "Coming soon", "Wkrótce", "În curând", "قريباً");
        Add("WaProfile.Engage.Title", "Waar staan jullie voor?", "What do you stand for?", "Za czym stoicie?", "Pentru ce sunteți?", "بماذا تقفون؟");
        Add("WaProfile.Engage.Lead", "Maatschappelijke betrokkenheid volgt in een volgende stap. Je kunt dit nu overslaan.", "Social engagement follows in a later step. You can skip this for now.", "Zaangażowanie społeczne będzie w kolejnym kroku. Możesz pominąć.", "Implicarea socială urmează. Poți sări peste.", "المشاركة المجتمعية تأتي لاحقاً. يمكنك التخطي الآن.");
        Add("WaProfile.Engage.NextVerify", "Verder naar verifiëren", "Continue to verify", "Dalej do weryfikacji", "Continuă la verificare", "متابعة إلى التحقق");

        Add("WaProfile.Vacancy.InheritTitle", "Cultuurprofiel van {0} gebruiken", "Use the culture profile of {0}", "Użyj profilu kultury {0}", "Folosește profilul cultural al {0}", "استخدم ملف ثقافة {0}");
        Add("WaProfile.Vacancy.InheritHint", "Standaard gebruikt elke vacature het cultuurprofiel van het bedrijf. Werkt dit team anders? Pas het dan alleen hier aan.", "By default every vacancy uses the company culture profile. Does this team work differently? Adjust it here only.", "Domyślnie oferta używa profilu firmy. Ten zespół działa inaczej? Zmień tylko tutaj.", "Implicit oferta folosește profilul firmei. Echipa lucrează altfel? Ajustează doar aici.", "افتراضياً تستخدم كل وظيفة ملف الشركة. هل يعمل هذا الفريق بشكل مختلف؟ عدّل هنا فقط.");
        Add("WaProfile.Vacancy.Override", "Dit team werkt anders", "This team works differently", "Ten zespół działa inaczej", "Această echipă lucrează altfel", "هذا الفريق يعمل بشكل مختلف");
        Add("WaProfile.Vacancy.OverrideHint", "Kies 3 tot 5 kenmerken voor dit team. Die gaan dan vóór het bedrijfsprofiel.", "Choose 3 to 5 traits for this team. They override the company profile.", "Wybierz 3–5 cech. Nadpiszą profil firmy.", "Alege 3–5 trăsături. Ele au prioritate față de profil.", "اختر 3 إلى 5 سمات. تتقدّم على ملف الشركة.");
        Add("WaProfile.Vacancy.Optional", "optioneel", "optional", "opcjonalne", "opțional", "اختياري");
        Add("WaProfile.Vacancy.NoProfile", "Nog geen cultuurprofiel. Vul ‘Zo werken wij’ in (1 minuut).", "No culture profile yet. Fill in ‘How we work’ (1 minute).", "Brak profilu kultury. Uzupełnij ‘Jak pracujemy’ (1 min).", "Niciun profil cultural. Completează ‘Cum lucrăm’ (1 min).", "لا ملف ثقافة بعد. عبّئ ‘كيف نعمل’ (دقيقة).");
        Add("WaProfile.Vacancy.EditLink", "Aanpassen", "Edit", "Edytuj", "Editează", "تعديل");
        Add("WaProfile.Vacancy.Section", "Cultuur van dit team", "This team's culture", "Kultura tego zespołu", "Cultura acestei echipe", "ثقافة هذا الفريق");
    }
}
