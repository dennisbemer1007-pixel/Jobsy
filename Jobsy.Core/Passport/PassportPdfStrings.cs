using Jobsy.Core.Localization;

namespace Jobsy.Core.Passport;

/// <summary>
/// PDF copy for nl, en, pl, ro and ar. One row is one key, so a language cannot be forgotten.
/// Index: 0 nl, 1 en, 2 pl, 3 ro, 4 ar.
/// </summary>
public static class PassportPdfStrings
{
    public static IReadOnlyCollection<string> Keys => Map.Keys;

    public static string T(string? language, string key)
    {
        if (!Map.TryGetValue(key, out var row))
        {
            return key;
        }

        return row[Index(language)];
    }

    public static string F(string? language, string key, params object[] args)
        => string.Format(System.Globalization.CultureInfo.InvariantCulture, T(language, key), args);

    public static bool HasKey(string key) => Map.ContainsKey(key);

    private static int Index(string? language) => JobsyLanguages.Normalize(language) switch
    {
        "en" => 1,
        "pl" => 2,
        "ro" => 3,
        "ar" => 4,
        _ => 0
    };

    private static readonly Dictionary<string, string[]> Map = new(StringComparer.Ordinal)
    {
        ["Title"] = ["DNA-paspoort", "DNA passport", "Paszport DNA", "Pașaport ADN", "جواز الحمض النووي"],
        ["Page1"] = ["Wie je bent", "Who you are", "Kim jesteś", "Cine ești", "من أنت"],
        ["Page2"] = ["Wat je hebt gedaan", "What you have done", "Co już robiłeś", "Ce ai făcut", "ما قمت به"],
        ["Badge"] = ["Lobsy-compleet", "Lobsy complete", "Lobsy kompletne", "Lobsy complet", "لوبسي مكتمل"],
        ["TestsProgress"] = ["{0}/4 DNA-tests afgerond", "{0}/4 DNA tests done", "{0}/4 testy DNA ukończone", "{0}/4 teste ADN gata", "{0}/4 اختبارات الحمض النووي مكتملة"],
        ["OpenForWork"] = ["Open voor werk", "Open for work", "Otwarty na pracę", "Deschis pentru lucru", "متاح للعمل"],
        ["PassportNo"] = ["Paspoortnr. {0}", "Passport no. {0}", "Nr paszportu {0}", "Nr. pașaport {0}", "رقم الجواز {0}"],
        ["Available"] = ["Beschikbaar per", "Available from", "Dostępny od", "Disponibil din", "متاح من"],
        ["Direct"] = ["Direct", "Right away", "Od razu", "Imediat", "فوراً"],
        ["Hours"] = ["{0} uur per week", "{0} hours a week", "{0} godz. tygodniowo", "{0} ore pe săptămână", "{0} ساعة في الأسبوع"],
        ["Shifts"] = ["Diensten", "Shifts", "Zmiany", "Ture", "الورديات"],
        ["ShiftYes"] = ["Ja", "Yes", "Tak", "Da", "نعم"],
        ["ShiftConsult"] = ["In overleg", "To agree", "Do uzgodnienia", "De discutat", "بالاتفاق"],
        ["ShiftNo"] = ["Nee", "No", "Nie", "Nu", "لا"],
        ["Part.Ochtend"] = ["Ocht.", "Morn.", "Rano", "Dim.", "صباح"],
        ["Part.Middag"] = ["Midd.", "Aft.", "Pop.", "Prânz", "ظهر"],
        ["Part.Avond"] = ["Avond", "Eve", "Wiecz.", "Seară", "مساء"],
        ["Part.Nacht"] = ["Nacht", "Night", "Noc", "Noapte", "ليل"],
        ["No.Ochtend"] = ["geen ochtenden", "no mornings", "bez poranków", "fără dimineți", "بدون صباح"],
        ["No.Middag"] = ["geen middagen", "no afternoons", "bez popołudni", "fără prânz", "بدون ظهر"],
        ["No.Avond"] = ["geen avonden", "no evenings", "bez wieczorów", "fără seri", "بدون مساء"],
        ["No.Nacht"] = ["geen nachten", "no nights", "bez nocy", "fără nopți", "بدون ليل"],
        ["Consult.Ochtend"] = ["ochtend in overleg", "morning to agree", "rano do uzgodnienia", "dimineața de discutat", "الصباح بالاتفاق"],
        ["Consult.Middag"] = ["middag in overleg", "afternoon to agree", "popołudnie do uzgodnienia", "prânzul de discutat", "الظهر بالاتفاق"],
        ["Consult.Avond"] = ["avond in overleg", "evening to agree", "wieczór do uzgodnienia", "seara de discutat", "المساء بالاتفاق"],
        ["Consult.Nacht"] = ["nacht in overleg", "night to agree", "noc do uzgodnienia", "noaptea de discutat", "الليل بالاتفاق"],
        ["TimesConsult"] = ["Tijden in overleg", "Times to agree", "Godziny do uzgodnienia", "Ore de discutat", "الأوقات بالاتفاق"],
        ["Transport"] = ["Vervoer", "Transport", "Dojazd", "Transport", "التنقل"],
        ["Licence"] = ["Rijbewijs {0}", "Licence {0}", "Prawo jazdy {0}", "Permis {0}", "رخصة {0}"],
        ["MaxTravel"] = ["max. {0} min reizen", "max. {0} min travel", "maks. {0} min dojazdu", "max. {0} min deplasare", "حد أقصى {0} دقائق"],
        ["OwnCar"] = ["Eigen auto: {0}", "Own car: {0}", "Własne auto: {0}", "Mașină proprie: {0}", "سيارة خاصة: {0}"],
        ["Yes"] = ["ja", "yes", "tak", "da", "نعم"],
        ["No"] = ["nee", "no", "nie", "nu", "لا"],
        ["Contact"] = ["Contact", "Contact", "Kontakt", "Contact", "اتصال"],
        ["WhatsApp"] = ["WhatsApp mag", "WhatsApp is ok", "WhatsApp można", "WhatsApp e ok", "واتساب مسموح"],
        ["Languages"] = ["Talen", "Languages", "Języki", "Limbi", "اللغات"],
        ["Dutch"] = ["Nederlands", "Dutch", "Niderlandzki", "Neerlandeză", "الهولندية"],
        ["WorkPrefs"] = ["Werkvoorkeuren", "Work preferences", "Preferencje pracy", "Preferințe de lucru", "تفضيلات العمل"],
        ["Strengths"] = ["Sterke punten", "Strengths", "Mocne strony", "Puncte forte", "نقاط القوة"],
        ["StrengthSource"] = ["Zelfinzicht uit een afgeronde test · {0}", "From a completed test · {0}", "Z ukończonego testu · {0}", "Dintr-un test terminat · {0}", "من اختبار مكتمل · {0}"],
        ["Seeks"] = ["Zoekt", "Looking for", "Szuka", "Caută", "يبحث عن"],
        ["Experience"] = ["Ervaring", "Experience", "Doświadczenie", "Experiență", "الخبرة"],
        ["Papers"] = ["Papieren", "Papers", "Dokumenty", "Acte", "الأوراق"],
        ["Certificates"] = ["Certificaten", "Certificates", "Certyfikaty", "Certificate", "الشهادات"],
        ["Education"] = ["Opleiding", "Education", "Wykształcenie", "Educație", "التعليم"],
        ["DnaTitle"] = ["Mijn DNA in 4 lagen", "My DNA in 4 layers", "Moje DNA w 4 warstwach", "ADN-ul meu în 4 straturi", "حمضي النووي في 4 طبقات"],
        ["Layer.competence"] = ["Competenties", "Competencies", "Kompetencje", "Competențe", "الكفاءات"],
        ["Layer.career"] = ["Loopbaan", "Career", "Kariera", "Carieră", "المسار"],
        ["Layer.culture"] = ["Werkcultuur", "Work culture", "Kultura pracy", "Cultura muncii", "ثقافة العمل"],
        ["Layer.values"] = ["Waarden", "Values", "Wartości", "Valori", "القيم"],
        ["NotDone"] = ["Nog niet gedaan", "Not done yet", "Jeszcze nie zrobione", "Încă nefăcut", "لم يُنجز بعد"],
        ["Done"] = ["Afgerond", "Done", "Ukończone", "Gata", "مكتمل"],
        ["HowIWork"] = ["Hoe ik graag werk", "How I like to work", "Jak lubię pracować", "Cum îmi place să lucrez", "كيف أحب أن أعمل"],
        ["OwnWords"] = ["In mijn eigen woorden", "In my own words", "Własnymi słowami", "Cu vorbele mele", "بكلماتي"],
        ["Motivation"] = ["Motivatie", "Motivation", "Motywacja", "Motivație", "الدافع"],
        ["Means"] = ["Wat betekent Lobsy-compleet?", "What does Lobsy complete mean?", "Co znaczy Lobsy kompletne?", "Ce înseamnă Lobsy complet?", "ماذا يعني لوبسي مكتمل؟"],
        ["Checked"] = ["Gecontroleerd door Lobsy", "Checked by Lobsy", "Sprawdzone przez Lobsy", "Verificat de Lobsy", "راجعته لوبسي"],
        ["NotChecked"] = ["Niet gecontroleerd", "Not checked", "Niesprawdzone", "Neverificat", "لم يُراجع"],
        ["EmailOk"] = ["E-mail bevestigd", "Email confirmed", "E-mail potwierdzony", "E-mail confirmat", "تم تأكيد البريد"],
        ["PhoneOk"] = ["Telefoon bevestigd", "Phone confirmed", "Telefon potwierdzony", "Telefon confirmat", "تم تأكيد الهاتف"],
        ["NotIdentity"] = ["Identiteit en werkvergunning", "Identity and work permit", "Tożsamość i pozwolenie na pracę", "Identitate și permis de muncă", "الهوية وتصريح العمل"],
        ["NotDiplomas"] = ["Echtheid van diploma's en certificaten", "Whether diplomas and certificates are genuine", "Autentyczność dyplomów i certyfikatów", "Autenticitatea diplomelor și certificatelor", "صحة الشهادات والوثائق"],
        ["NotReferences"] = ["Referenties van vorige werkgevers", "References from previous employers", "Referencje od poprzednich pracodawców", "Referințe de la foști angajatori", "مراجع من أصحاب العمل السابقين"],
        ["FooterPrivacy"] = ["Bewust niet op dit paspoort: geboortedatum, foto, nationaliteit, BSN, gezondheid.", "Left off on purpose: date of birth, photo, nationality, citizen service number, health.", "Świadomie pominięte: data urodzenia, zdjęcie, narodowość, BSN, zdrowie.", "Lăsate deoparte intenționat: data nașterii, foto, naționalitate, BSN, sănătate.", "تُرك عمداً: تاريخ الميلاد، الصورة، الجنسية، رقم الخدمة، الصحة."],
        ["FooterNoScore"] = ["Alleen feiten die jij zelf invulde. Geen score, geen ranking en geen automatische selectie.", "Only facts you entered yourself. No score, no ranking and no automatic selection.", "Tylko fakty, które sam wpisałeś. Bez wyniku, bez rankingu i bez automatycznego wyboru.", "Doar fapte pe care le-ai scris tu. Fără scor, fără clasament și fără selecție automată.", "فقط حقائق كتبتها أنت. بلا درجة، بلا ترتيب وبلا اختيار تلقائي."],
        ["FooterLive"] = ["Bekijk de live versie in Lobsy.", "See the live version in Lobsy.", "Zobacz wersję na żywo w Lobsy.", "Vezi versiunea live în Lobsy.", "انظر النسخة الحية في لوبسي."],
        ["More"] = ["+{0} meer in Lobsy", "+{0} more in Lobsy", "+{0} więcej w Lobsy", "+{0} mai mult în Lobsy", "+{0} المزيد في لوبسي"],
        ["ExperienceCount"] = ["{0} eerdere werkgevers", "{0} earlier employers", "{0} wcześniejszych pracodawców", "{0} angajatori anteriori", "{0} أصحاب عمل سابقين"],
        ["Indoor"] = ["Binnen", "Indoors", "Wewnątrz", "În interior", "في الداخل"],
        ["Outdoor"] = ["Buiten", "Outdoors", "Na zewnątrz", "Afară", "في الخارج"],
        ["Physical"] = ["Fysiek werk", "Physical work", "Praca fizyczna", "Muncă fizică", "عمل بدني"],
        ["Pace"] = ["Tempo", "Pace", "Tempo", "Ritm", "الوتيرة"],
        ["Environment"] = ["Werkomgeving", "Workplace", "Środowisko pracy", "Mediul de lucru", "بيئة العمل"],
        ["Work.Indoor.prefer"] = ["voorkeur", "prefer", "wolę", "prefer", "أفضل"],
        ["Work.Indoor.ok"] = ["prima", "fine", "ok", "ok", "مناسب"],
        ["Work.Indoor.rather-not"] = ["liever niet", "rather not", "raczej nie", "mai degrabă nu", "أفضل ألا"],
        ["Work.Outdoor.prefer"] = ["voorkeur", "prefer", "wolę", "prefer", "أفضل"],
        ["Work.Outdoor.ok"] = ["prima", "fine", "ok", "ok", "مناسب"],
        ["Work.Outdoor.ok-not-frost"] = ["prima, niet bij vorst", "fine, not in frost", "ok, nie przy mrozie", "ok, nu pe ger", "مناسب، ليس في الصقيع"],
        ["Work.Outdoor.rather-not"] = ["liever niet", "rather not", "raczej nie", "mai degrabă nu", "أفضل ألا"],
        ["Work.Physical.light"] = ["licht werk", "light work", "lekka praca", "muncă ușoară", "عمل خفيف"],
        ["Work.Physical.standing"] = ["staand werk", "standing work", "praca na stojąco", "muncă în picioare", "عمل وقوفاً"],
        ["Work.Physical.lifting-15"] = ["tillen tot 15 kg", "lifting up to 15 kg", "dźwiganie do 15 kg", "ridicare până la 15 kg", "رفع حتى 15 كغ"],
        ["Work.Physical.lifting-25"] = ["tillen tot 25 kg", "lifting up to 25 kg", "dźwiganie do 25 kg", "ridicare până la 25 kg", "رفع حتى 25 كغ"],
        ["Work.Pace.norm-ok"] = ["prima", "a normal pace is fine", "normalne tempo jest ok", "ritmul normal e ok", "الوتيرة العادية مناسبة"],
        ["Work.Pace.calm"] = ["rustig tempo", "a calm pace", "spokojne tempo", "ritm calm", "وتيرة هادئة"],
        ["Contract.uitzend"] = ["uitzend", "agency", "agencja", "agenție", "وكالة"],
        ["Contract.vast"] = ["vast", "permanent", "stały", "nedeterminat", "دائم"],
        ["Contract.tijdelijk"] = ["tijdelijk", "temporary", "tymczasowy", "temporar", "مؤقت"],
        ["Contract.seizoen"] = ["seizoen", "seasonal", "sezon", "sezon", "موسمي"],
        ["Contract.oproep"] = ["oproep", "on-call", "na wezwanie", "la chemare", "عند الطلب"],
        ["Contract.geen-voorkeur"] = ["geen voorkeur", "no preference", "bez preferencji", "fără preferință", "بدون تفضيل"],
        ["Dutch.beginner"] = ["Beginner", "Beginner", "Początkujący", "Începător", "مبتدئ"],
        ["Dutch.basis"] = ["Basis", "Basic", "Podstawowy", "De bază", "أساسي"],
        ["Dutch.goed"] = ["Goed", "Good", "Dobry", "Bun", "جيد"],
        ["Dutch.vloeiend"] = ["Vloeiend", "Fluent", "Płynny", "Fluent", "بطلاقة"],
        ["Dutch.moedertaal"] = ["Moedertaal", "Native", "Ojczysty", "Limbă maternă", "لغة أم"],
        ["DutchNote.beginner"] = ["Leert het net (A1–A2).", "Just starting (A1–A2).", "Dopiero się uczy (A1–A2).", "Abia învață (A1–A2).", "يتعلم للتو (A1–A2)."],
        ["DutchNote.basis"] = ["Redt zich in gewone gesprekken (B1).", "Manages in everyday talks (B1).", "Radzi sobie w zwykłych rozmowach (B1).", "Se descurcă în conversații obișnuite (B1).", "يتعامل في المحادثات العادية (B1)."],
        ["DutchNote.goed"] = ["Praat vlot over werk (B2).", "Speaks easily about work (B2).", "Swobodnie mówi o pracy (B2).", "Vorbește lejer despre muncă (B2).", "يتحدث بسهولة عن العمل (B2)."],
        ["DutchNote.vloeiend"] = ["Bijna als een moedertaal (C1).", "Almost like a native speaker (C1).", "Prawie jak język ojczysty (C1).", "Aproape ca limba maternă (C1).", "تقريباً كلغة أم (C1)."],
        ["DutchNote.moedertaal"] = ["Nederlands is de moedertaal.", "Dutch is the native language.", "Niderlandzki to język ojczysty.", "Neerlandeza este limba maternă.", "الهولندية هي اللغة الأم."],
        ["Employer.small-team"] = ["klein, warm team", "small, warm team", "mały, ciepły zespół", "echipă mică și caldă", "فريق صغير ودافئ"],
        ["Employer.large-company"] = ["groot bedrijf", "large company", "duża firma", "companie mare", "شركة كبيرة"],
        ["Employer.fixed-workplace"] = ["vaste werkplek", "fixed workplace", "stałe miejsce pracy", "loc de muncă fix", "مكان عمل ثابت"],
        ["Employer.learn-on-the-job"] = ["leren op de werkvloer", "learn on the job", "nauka w pracy", "învățare la locul de muncă", "التعلم في العمل"],
        ["Employer.dutch-support"] = ["hulp met Nederlands", "help with Dutch", "pomoc z niderlandzkim", "ajutor cu neerlandeza", "مساعدة في الهولندية"],
        ["Employer.growth"] = ["kans om door te groeien", "chance to grow", "szansa na rozwój", "șansă de creștere", "فرصة للتطور"],
        ["Employer.close-to-home"] = ["dicht bij huis", "close to home", "blisko domu", "aproape de casă", "قريب من المنزل"],
        ["Employer.variety"] = ["veel afwisseling", "lots of variety", "dużo różnorodności", "multă varietate", "تنوع كبير"],
        ["Transport.Fiets"] = ["fiets", "bike", "rower", "bicicletă", "دراجة"],
        ["Transport.E-bike"] = ["e-bike", "e-bike", "e-bike", "e-bike", "دراجة كهربائية"],
        ["Transport.Auto"] = ["auto", "car", "auto", "mașină", "سيارة"],
        ["Transport.OV"] = ["ov", "public transport", "komunikacja", "transport public", "مواصلات"],
        ["Transport.Lopend"] = ["lopend", "walking", "pieszo", "pe jos", "مشياً"],
        ["Lang.nl"] = ["Nederlands", "Dutch", "Niderlandzki", "Neerlandeză", "الهولندية"],
        ["Lang.en"] = ["Engels", "English", "Angielski", "Engleză", "الإنجليزية"],
        ["Lang.pl"] = ["Pools", "Polish", "Polski", "Poloneză", "البولندية"],
        ["Lang.ro"] = ["Roemeens", "Romanian", "Rumuński", "Română", "الرومانية"],
        ["Lang.ar"] = ["Arabisch", "Arabic", "Arabski", "Arabă", "العربية"],
        ["Lang.de"] = ["Duits", "German", "Niemiecki", "Germană", "الألمانية"],
        ["Lang.fr"] = ["Frans", "French", "Francuski", "Franceză", "الفرنسية"],
        ["Lang.tr"] = ["Turks", "Turkish", "Turecki", "Turcă", "التركية"],
        ["Lang.uk"] = ["Oekraïens", "Ukrainian", "Ukraiński", "Ucraineană", "الأوكرانية"],
        ["Lang.es"] = ["Spaans", "Spanish", "Hiszpański", "Spaniolă", "الإسبانية"],
        ["Lang.so"] = ["Somalisch", "Somali", "Somalijski", "Somaleză", "الصومالية"],
        ["Lang.ti"] = ["Tigrinya", "Tigrinya", "Tigrinia", "Tigrinya", "التغرينية"],
        ["Lang.fa"] = ["Perzisch", "Persian", "Perski", "Persană", "الفارسية"]
    };
}
