namespace Jobsy.Web.Localization;

/// <summary>
/// Candidate contact-request copy (04). Honest by design: nothing is shared until the candidate
/// confirms in the share dialog, and saying no only tells the employer "no interest".
/// </summary>
public static class UiStringsTalentCandidate
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

        // --- words used inside other lines (D8: paspoort vs profiel) ---
        Add("TalentC.Word.Passport",
            "paspoort",
            "passport",
            "paszport",
            "pașaport",
            "جواز");
        Add("TalentC.Word.Profile",
            "profiel",
            "profile",
            "profil",
            "profil",
            "ملف");

        // --- page chrome ---
        Add("TalentC.Eyebrow",
            "Contactverzoeken",
            "Contact requests",
            "Zapytania o kontakt",
            "Cereri de contact",
            "طلبات التواصل");
        Add("TalentC.Title",
            "Een werkgever wil je spreken",
            "An employer wants to talk to you",
            "Pracodawca chce z tobą porozmawiać",
            "Un angajator vrea să vorbească cu tine",
            "صاحب عمل يريد التحدث إليك");
        Add("TalentC.TitleNone",
            "Contactverzoeken",
            "Contact requests",
            "Zapytania o kontakt",
            "Cereri de contact",
            "طلبات التواصل");
        Add("TalentC.Lead",
            "Werkgevers zien je naam, e-mail en telefoon pas als jij ja zegt. Nee zeggen mag altijd.",
            "Employers only see your name, e-mail and phone once you say yes. You may always say no.",
            "Pracodawcy zobaczą twoje imię, e-mail i telefon dopiero, gdy powiesz tak. Zawsze możesz odmówić.",
            "Angajatorii îți văd numele, e-mailul și telefonul doar după ce spui da. Poți refuza oricând.",
            "لا يرى أصحاب العمل اسمك وبريدك وهاتفك إلا بعد موافقتك. يمكنك الرفض دائماً.");

        // --- how it works (left card / mobile disclosure) ---
        Add("TalentC.How.Title",
            "Zo werkt het",
            "How this works",
            "Jak to działa",
            "Cum funcționează",
            "كيف يعمل هذا");
        Add("TalentC.How.Sub",
            "Jij beslist, altijd.",
            "You decide, always.",
            "Ty decydujesz, zawsze.",
            "Tu decizi, mereu.",
            "القرار لك، دائماً.");
        Add("TalentC.How.Anonymous",
            "Een werkgever ziet je {0}, maar niet je naam, e-mail of telefoon.",
            "An employer sees your {0}, but not your name, e-mail or phone.",
            "Pracodawca widzi twój {0}, ale nie imię, e-mail ani telefon.",
            "Un angajator vede {0}, dar nu numele, e-mailul sau telefonul.",
            "يرى صاحب العمل {0} الخاص بك، لكن ليس اسمك أو بريدك أو هاتفك.");
        Add("TalentC.How.Share",
            "Pas als jij ja zegt, krijgt die werkgever je naam, e-mail en telefoon.",
            "Only when you say yes does that employer get your name, e-mail and phone.",
            "Dopiero gdy powiesz tak, ten pracodawca otrzyma twoje imię, e-mail i telefon.",
            "Doar când spui da, acel angajator primește numele, e-mailul și telefonul tău.",
            "عند موافقتك فقط يحصل صاحب العمل على اسمك وبريدك وهاتفك.");
        Add("TalentC.How.Time",
            "Reageer het liefst binnen 2 dagen. Zeg je niets, dan wordt er niets gedeeld.",
            "Reply within 2 days if you can. Say nothing and nothing is shared.",
            "Odpowiedz najlepiej w ciągu 2 dni. Jeśli nic nie powiesz, nic nie zostanie udostępnione.",
            "Răspunde, de preferat, în 2 zile. Dacă nu spui nimic, nu se împarte nimic.",
            "يُفضَّل الرد خلال يومين. إذا لم ترد، فلن تُشارَك أي بيانات.");
        Add("TalentC.How.No",
            "Nee zeggen mag. De werkgever ziet alleen “geen interesse”.",
            "Saying no is fine. The employer only sees “no interest”.",
            "Możesz odmówić. Pracodawca zobaczy tylko „brak zainteresowania”.",
            "Poți spune nu. Angajatorul vede doar „fără interes”.",
            "الرفض مسموح. يرى صاحب العمل «لا يوجد اهتمام» فقط.");

        // --- request cards ---
        Add("TalentC.Company.Unknown",
            "Een werkgever",
            "An employer",
            "Pracodawca",
            "Un angajator",
            "صاحب عمل");
        Add("TalentC.Status.Open",
            "Wacht op jou",
            "Waiting for you",
            "Czeka na ciebie",
            "Așteaptă răspunsul tău",
            "بانتظار ردك");
        Add("TalentC.Status.Shared",
            "Je zei ja",
            "You said yes",
            "Powiedziałeś tak",
            "Ai spus da",
            "لقد وافقت");
        Add("TalentC.Status.Declined",
            "Je zei nee",
            "You said no",
            "Powiedziałeś nie",
            "Ai spus nu",
            "لقد رفضت");
        Add("TalentC.Status.Withdrawn",
            "Gestopt",
            "Stopped",
            "Zakończone",
            "Oprit",
            "متوقف");
        Add("TalentC.When.Before",
            "Reageer het liefst vóór {0}",
            "Reply before {0} if you can",
            "Odpowiedz najlepiej przed {0}",
            "Răspunde, de preferat, înainte de {0}",
            "يُفضَّل الرد قبل {0}");
        Add("TalentC.When.Late",
            "De tijd is om, maar je kunt nog reageren.",
            "Time is up, but you can still reply.",
            "Czas minął, ale nadal możesz odpowiedzieć.",
            "Timpul a trecut, dar poți încă răspunde.",
            "انتهى الوقت، لكن ما زال يمكنك الرد.");
        Add("TalentC.Shared.Line",
            "{0} · Je naam, e-mail en telefoon zijn gedeeld. De werkgever neemt contact met je op.",
            "{0} · Your name, e-mail and phone were shared. The employer will contact you.",
            "{0} · Twoje imię, e-mail i telefon zostały udostępnione. Pracodawca się z tobą skontaktuje.",
            "{0} · Numele, e-mailul și telefonul tău au fost partajate. Angajatorul te va contacta.",
            "{0} · تمت مشاركة اسمك وبريدك وهاتفك. سيتواصل صاحب العمل معك.");
        Add("TalentC.Declined.Line",
            "{0} · Er is niets gedeeld.",
            "{0} · Nothing was shared.",
            "{0} · Nic nie zostało udostępnione.",
            "{0} · Nu s-a partajat nimic.",
            "{0} · لم تُشارَك أي بيانات.");
        Add("TalentC.Declined.Placed",
            "· Je had al werk",
            "· You already had work",
            "· Miałeś już pracę",
            "· Aveai deja de lucru",
            "· كان لديك عمل بالفعل");
        Add("TalentC.Withdrawn.Line",
            "De werkgever heeft het verzoek ingetrokken. Er is niets gedeeld.",
            "The employer withdrew the request. Nothing was shared.",
            "Pracodawca wycofał zapytanie. Nic nie zostało udostępnione.",
            "Angajatorul a retras cererea. Nu s-a partajat nimic.",
            "سحب صاحب العمل الطلب. لم تُشارَك أي بيانات.");
        Add("TalentC.Said.No",
            "Je zei nee. Er is niets gedeeld.",
            "You said no. Nothing was shared.",
            "Powiedziałeś nie. Nic nie zostało udostępnione.",
            "Ai spus nu. Nu s-a partajat nimic.",
            "لقد رفضت. لم تُشارَك أي بيانات.");

        // --- actions ---
        Add("TalentC.Action.Yes",
            "Ja, deel mijn gegevens",
            "Yes, share my details",
            "Tak, udostępnij moje dane",
            "Da, împarte datele mele",
            "نعم، شارك بياناتي");
        Add("TalentC.Action.Placed",
            "Ik heb al werk",
            "I already have work",
            "Mam już pracę",
            "Am deja de lucru",
            "لدي عمل بالفعل");
        Add("TalentC.Action.No",
            "Geen interesse",
            "Not interested",
            "Brak zainteresowania",
            "Fără interes",
            "لا يوجد اهتمام");

        // --- empty state and the lobster ---
        Add("TalentC.Empty.Text",
            "Nog geen contactverzoeken. Maak je {0} sterker, dan vinden werkgevers je sneller.",
            "No contact requests yet. Make your {0} stronger and employers will find you faster.",
            "Jeszcze żadnych zapytań. Wzmocnij swój {0}, a pracodawcy znajdą cię szybciej.",
            "Încă nicio cerere. Întărește-ți {0} și angajatorii te vor găsi mai repede.",
            "لا توجد طلبات بعد. عزّز {0} الخاص بك ليجدك أصحاب العمل أسرع.");
        Add("TalentC.Empty.Link",
            "Naar mijn {0}",
            "To my {0}",
            "Do mojego {0}",
            "Către {0} meu",
            "إلى {0}");
        Add("TalentC.Say",
            "Mijn antennes trillen: iemand vindt dat je past. Jij beslist of je ja zegt.",
            "My antennas are buzzing: someone thinks you fit. You decide whether to say yes.",
            "Moje czułki drżą: ktoś uważa, że pasujesz. Ty decydujesz, czy powiesz tak.",
            "Antenele mele vibrează: cineva crede că te potrivești. Tu decizi dacă spui da.",
            "قرون استشعاري تهتزّ: أحدهم يرى أنك مناسب. أنت تقرر إن كنت توافق.");
        Add("TalentC.Say.None",
            "Ik luister mee. Komt er een verzoek, dan zie je het hier.",
            "I’m listening. If a request arrives, you’ll see it here.",
            "Nasłuchuję. Gdy pojawi się zapytanie, zobaczysz je tutaj.",
            "Ascult. Când apare o cerere, o vezi aici.",
            "أنا أستمع. عند وصول طلب ستراه هنا.");

        // --- share confirm dialog (cr-d7) ---
        Add("TalentC.Dialog.Title",
            "Je gegevens delen met {0}?",
            "Share your details with {0}?",
            "Udostępnić twoje dane firmie {0}?",
            "Împarți datele tale cu {0}?",
            "مشاركة بياناتك مع {0}؟");
        Add("TalentC.Dialog.Sub",
            "Dit krijgt {0} als je ja zegt:",
            "This is what {0} gets when you say yes:",
            "To otrzyma {0}, gdy powiesz tak:",
            "Asta primește {0} când spui da:",
            "هذا ما سيحصل عليه {0} عند موافقتك:");
        Add("TalentC.Dialog.Name",
            "Naam",
            "Name",
            "Imię",
            "Nume",
            "الاسم");
        Add("TalentC.Dialog.Email",
            "E-mail",
            "E-mail",
            "E-mail",
            "E-mail",
            "البريد الإلكتروني");
        Add("TalentC.Dialog.Phone",
            "Telefoon",
            "Phone",
            "Telefon",
            "Telefon",
            "الهاتف");
        Add("TalentC.Dialog.Missing",
            "niet ingevuld",
            "not provided",
            "nie podano",
            "necompletat",
            "غير مُدخل");
        Add("TalentC.Dialog.Hint",
            "Delen kun je niet terugdraaien. De werkgever mag je daarna bellen of mailen over dit werk.",
            "Sharing cannot be undone. After that the employer may call or e-mail you about this work.",
            "Udostępnienia nie można cofnąć. Potem pracodawca może zadzwonić lub napisać w sprawie tej pracy.",
            "Partajarea nu poate fi anulată. După aceea angajatorul poate să te sune sau să îți scrie despre acest job.",
            "لا يمكن التراجع عن المشاركة. بعدها يمكن لصاحب العمل الاتصال بك أو مراسلتك بشأن هذا العمل.");
        Add("TalentC.Dialog.Cancel",
            "Nog niet",
            "Not yet",
            "Jeszcze nie",
            "Nu încă",
            "ليس بعد");
        Add("TalentC.Shared.Announce",
            "Je gegevens zijn gedeeld met {0}.",
            "Your details were shared with {0}.",
            "Twoje dane zostały udostępnione firmie {0}.",
            "Datele tale au fost partajate cu {0}.",
            "تمت مشاركة بياناتك مع {0}.");

        // --- gate off ---
        Add("TalentC.Off.Title",
            "Contactverzoeken staan nu uit.",
            "Contact requests are switched off right now.",
            "Zapytania o kontakt są teraz wyłączone.",
            "Cererile de contact sunt momentan oprite.",
            "طلبات التواصل متوقفة حالياً.");

        // --- errors (codes, never raw API text) ---
        Add("TalentC.Err.NotFound",
            "Dit verzoek bestaat niet meer.",
            "This request no longer exists.",
            "To zapytanie już nie istnieje.",
            "Această cerere nu mai există.",
            "هذا الطلب لم يعد موجوداً.");
        Add("TalentC.Err.CannotRespond",
            "Op dit verzoek kun je niet meer reageren.",
            "You can no longer reply to this request.",
            "Nie możesz już odpowiedzieć na to zapytanie.",
            "Nu mai poți răspunde la această cerere.",
            "لم يعد بإمكانك الرد على هذا الطلب.");
        Add("TalentC.Err.ConfirmShare",
            "Bevestig eerst in het venster dat je je gegevens wilt delen.",
            "First confirm in the dialog that you want to share your details.",
            "Najpierw potwierdź w okienku, że chcesz udostępnić swoje dane.",
            "Confirmă mai întâi în fereastră că vrei să împarți datele tale.",
            "أكّد أولاً في النافذة أنك تريد مشاركة بياناتك.");
        Add("TalentC.Err.Preview",
            "We kunnen nu niet laten zien wat er gedeeld wordt. Probeer het straks opnieuw.",
            "We can’t show what would be shared right now. Please try again later.",
            "Nie możemy teraz pokazać, co zostanie udostępnione. Spróbuj później.",
            "Nu putem arăta acum ce s-ar partaja. Încearcă mai târziu.",
            "لا يمكننا الآن إظهار ما سيُشارَك. حاول لاحقاً.");

        // --- employer side: the reason next to a declined request (§1) ---
        Add("Talent.DeclineReason.NotInterested",
            "Geen interesse",
            "Not interested",
            "Brak zainteresowania",
            "Fără interes",
            "لا يوجد اهتمام");
        Add("Talent.DeclineReason.AlreadyPlaced",
            "Al voorzien",
            "Already placed",
            "Już zatrudniony",
            "Deja ocupat",
            "تم التعيين بالفعل");
    }
}
