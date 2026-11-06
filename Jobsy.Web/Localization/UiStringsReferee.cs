namespace Jobsy.Web.Localization;

internal static class UiStringsReferee
{
    public static void MergeAll(
        IDictionary<string, string> nl,
        IDictionary<string, string> en,
        IDictionary<string, string> pl,
        IDictionary<string, string> ro,
        IDictionary<string, string> ar)
    {
        void Add(string key, string n, string e, string p, string r, string a)
        {
            nl[key] = n;
            en[key] = e;
            pl[key] = p;
            ro[key] = r;
            ar[key] = a;
        }

        Add("Referee.Ask",
            "Vraag bevestiging",
            "Ask for confirmation",
            "Poproś o potwierdzenie",
            "Cere confirmarea",
            "اطلب التأكيد");
        Add("Referee.AskHint",
            "We mailen 5 korte vragen. Jij kiest later wat een partner ziet.",
            "We email 5 short questions. You choose later what a partner sees.",
            "Wyślemy 5 krótkich pytań. Potem sam wybierasz, co widzi partner.",
            "Trimitem 5 întrebări scurte. Tu alegi mai târziu ce vede un partener.",
            "نرسل 5 أسئلة قصيرة. أنت تختار لاحقاً ما يراه الشريك.");
        Add("Referee.Role",
            "Welk werk deed je daar?",
            "What work did you do there?",
            "Jaką pracę tam robiłeś?",
            "Ce muncă ai făcut acolo?",
            "ما العمل الذي قمت به هناك؟");
        Add("Referee.Consent",
            "Ik vraag deze persoon om te bevestigen dat ik daar werkte. Lobsy stuurt een mail met 5 korte vragen. Ik kies later zelf of een partner de antwoorden ziet.",
            "I ask this person to confirm that I worked there. Lobsy sends an email with 5 short questions. I choose later whether a partner sees the answers.",
            "Proszę tę osobę, by potwierdziła, że tam pracowałem. Lobsy wyśle mail z 5 krótkimi pytaniami. Sam wybiorę, czy partner zobaczy odpowiedzi.",
            "Rog această persoană să confirme că am lucrat acolo. Lobsy trimite un e-mail cu 5 întrebări scurte. Aleg eu mai târziu dacă un partener vede răspunsurile.",
            "أطلب من هذا الشخص أن يؤكد أنني عملت هناك. يرسل لوبسي بريداً فيه 5 أسئلة قصيرة. أختار لاحقاً إن كان الشريك يرى الإجابات.");
        Add("Referee.Send",
            "Verstuur de mail",
            "Send the email",
            "Wyślij e-mail",
            "Trimite e-mailul",
            "أرسل البريد");
        Add("Referee.Sent",
            "De mail is onderweg.",
            "The email is on its way.",
            "E-mail jest w drodze.",
            "E-mailul este pe drum.",
            "البريد في الطريق.");
        Add("Referee.Confirmed",
            "Bevestigd door referent",
            "Confirmed by the referee",
            "Potwierdzone przez osobę",
            "Confirmat de referent",
            "أكده المرجع");
        Add("Referee.Declined",
            "Deze persoon wil niet meedoen.",
            "This person does not want to take part.",
            "Ta osoba nie chce wziąć udziału.",
            "Această persoană nu vrea să participe.",
            "هذا الشخص لا يريد المشاركة.");
        Add("Referee.Pending",
            "We wachten op antwoord.",
            "We are waiting for an answer.",
            "Czekamy na odpowiedź.",
            "Așteptăm un răspuns.",
            "ننتظر الإجابة.");
        Add("Referee.Q1",
            "Klopt het dat {0} bij jullie werkte als {1}?",
            "Is it true that {0} worked with you as {1}?",
            "Czy to prawda, że {0} pracował u was jako {1}?",
            "Este adevărat că {0} a lucrat la voi ca {1}?",
            "هل صحيح أن {0} عمل لديكم بصفة {1}؟");
        Add("Referee.Yes",
            "Ja",
            "Yes",
            "Tak",
            "Da",
            "نعم");
        Add("Referee.No",
            "Nee",
            "No",
            "Nie",
            "Nu",
            "لا");
        Add("Referee.Q2",
            "Van wanneer tot wanneer ongeveer?",
            "From when until when, roughly?",
            "Mniej więcej od kiedy do kiedy?",
            "Cam de când până când?",
            "من متى إلى متى تقريباً؟");
        Add("Referee.Q3",
            "Wat deed {0} goed?",
            "What did {0} do well?",
            "Co {0} robił dobrze?",
            "Ce a făcut {0} bine?",
            "ماذا أجاد {0}؟");
        Add("Referee.Q4",
            "Zou je opnieuw met {0} willen werken?",
            "Would you work with {0} again?",
            "Czy chciałbyś znowu pracować z {0}?",
            "Ai vrea să lucrezi din nou cu {0}?",
            "هل تريد العمل مع {0} مرة أخرى؟");
        Add("Referee.Maybe",
            "Misschien",
            "Maybe",
            "Może",
            "Poate",
            "ربما");
        Add("Referee.Q5",
            "Wil je nog iets kwijt?",
            "Do you want to add anything?",
            "Chcesz jeszcze coś dodać?",
            "Vrei să mai spui ceva?",
            "هل تريد إضافة شيء؟");
        Add("Referee.Q5Hint",
            "Dit mag leeg blijven.",
            "You can leave this empty.",
            "To może zostać puste.",
            "Poți lăsa gol.",
            "يمكنك ترك هذا فارغاً.");
        Add("Referee.Submit",
            "Verstuur antwoorden",
            "Send answers",
            "Wyślij odpowiedzi",
            "Trimite răspunsurile",
            "أرسل الإجابات");
        Add("Referee.Decline",
            "Nee, ik doe niet mee",
            "No, I will not take part",
            "Nie, nie biorę udziału",
            "Nu, nu particip",
            "لا، لن أشارك");
        Add("Referee.Privacy",
            "Lobsy vraagt dit omdat deze persoon jou noemde. Je antwoorden gaan naar die persoon. Die persoon kiest of een partner ze ziet. We bewaren dit zolang het account bestaat. Je hoeft niet te antwoorden.",
            "Lobsy asks this because this person named you. Your answers go to that person. That person chooses whether a partner sees them. We keep this while the account exists. You do not have to answer.",
            "Lobsy pyta, bo ta osoba cię wskazała. Odpowiedzi idą do niej. Ta osoba wybiera, czy partner je widzi. Trzymamy to, dopóki konto istnieje. Nie musisz odpowiadać.",
            "Lobsy întreabă pentru că această persoană te-a numit. Răspunsurile merg la ea. Ea alege dacă un partener le vede. Le păstrăm cât există contul. Nu trebuie să răspunzi.",
            "يسأل لوبسي لأن هذا الشخص سمّاك. إجاباتك تذهب إليه. هو يختار إن كان الشريك يراها. نحتفظ بها ما دام الحساب موجوداً. لست مضطراً للإجابة.");
        Add("Referee.Misuse",
            "Melding van misbruik",
            "Report misuse",
            "Zgłoś nadużycie",
            "Semnalează un abuz",
            "الإبلاغ عن إساءة");
        Add("Referee.MisuseHint",
            "Klopt deze vraag niet? Schrijf kort wat er mis is.",
            "Is this request wrong? Write shortly what is wrong.",
            "Czy ta prośba jest zła? Napisz krótko, co jest nie tak.",
            "Cererea nu este corectă? Scrie scurt ce nu e bine.",
            "هل هذا الطلب غير صحيح؟ اكتب باختصار ما الخطأ.");
        Add("Referee.MisuseSend",
            "Verstuur melding",
            "Send report",
            "Wyślij zgłoszenie",
            "Trimite sesizarea",
            "أرسل البلاغ");
        Add("Referee.Thanks",
            "Bedankt. We hebben je antwoord.",
            "Thank you. We have your answer.",
            "Dziękujemy. Mamy twoją odpowiedź.",
            "Mulțumim. Avem răspunsul tău.",
            "شكراً. وصلنا جوابك.");
        Add("Referee.ThanksDecline",
            "Bedankt. Je doet niet mee.",
            "Thank you. You will not take part.",
            "Dziękujemy. Nie bierzesz udziału.",
            "Mulțumim. Nu participi.",
            "شكراً. لن تشارك.");
        Add("Referee.ThanksMisuse",
            "Bedankt. We hebben je melding.",
            "Thank you. We have your report.",
            "Dziękujemy. Mamy twoje zgłoszenie.",
            "Mulțumim. Avem sesizarea ta.",
            "شكراً. وصلنا بلاغك.");
        Add("Referee.Invalid",
            "Deze link werkt niet.",
            "This link does not work.",
            "Ten link nie działa.",
            "Acest link nu funcționează.",
            "هذا الرابط لا يعمل.");
        Add("Referee.Expired",
            "Deze link is verlopen.",
            "This link has expired.",
            "Ten link wygasł.",
            "Acest link a expirat.",
            "انتهت صلاحية هذا الرابط.");
        Add("Referee.Used",
            "Deze link is al gebruikt.",
            "This link was already used.",
            "Ten link został już użyty.",
            "Acest link a fost deja folosit.",
            "استُخدم هذا الرابط من قبل.");
        Add("Referee.AnswersMissing",
            "Vul de vragen in.",
            "Fill in the questions.",
            "Uzupełnij pytania.",
            "Completează întrebările.",
            "املأ الأسئلة.");
        Add("Referee.ShareTitle",
            "Wat ziet een partner?",
            "What does a partner see?",
            "Co widzi partner?",
            "Ce vede un partener?",
            "ماذا يرى الشريك؟");
        Add("Referee.ShareMaster",
            "Toon deze bevestiging in het partnerpaspoort",
            "Show this confirmation on the partner passport",
            "Pokaż to potwierdzenie w paszporcie partnera",
            "Arată această confirmare în pașaportul partenerului",
            "أظهر هذا التأكيد في جواز الشريك");
        Add("Referee.ShareWorked",
            "Toon of je daar werkte",
            "Show whether you worked there",
            "Pokaż, czy tam pracowałeś",
            "Arată dacă ai lucrat acolo",
            "أظهر إن كنت عملت هناك");
        Add("Referee.SharePeriod",
            "Toon de periode",
            "Show the period",
            "Pokaż okres",
            "Arată perioada",
            "أظهر الفترة");
        Add("Referee.ShareWell",
            "Toon wat je goed deed",
            "Show what you did well",
            "Pokaż, co robiłeś dobrze",
            "Arată ce ai făcut bine",
            "أظهر ما أجدته");
        Add("Referee.ShareAgain",
            "Toon of ze opnieuw met je willen werken",
            "Show whether they would work with you again",
            "Pokaż, czy chcą znowu z tobą pracować",
            "Arată dacă vor lucra din nou cu tine",
            "أظهر إن كانوا يريدون العمل معك مجدداً");
        Add("Referee.ShareExtra",
            "Toon de extra zin",
            "Show the extra line",
            "Pokaż dodatkowe zdanie",
            "Arată fraza extra",
            "أظهر الجملة الإضافية");
        Add("Referee.ShareSave",
            "Bewaar deze keuze",
            "Save this choice",
            "Zapisz ten wybór",
            "Salvează alegerea",
            "احفظ هذا الاختيار");
        Add("Referee.PageTitle",
            "Bevestig het werk",
            "Confirm the work",
            "Potwierdź pracę",
            "Confirmă munca",
            "أكد العمل");
        Add("Referee.Seo.Title",
            "Bevestig het werk",
            "Confirm the work",
            "Potwierdź pracę",
            "Confirmă munca",
            "أكد العمل");
        Add("Referee.Seo.Description",
            "Vijf korte vragen. Je hebt geen account nodig.",
            "Five short questions. You do not need an account.",
            "Pięć krótkich pytań. Konto nie jest potrzebne.",
            "Cinci întrebări scurte. Nu ai nevoie de cont.",
            "خمسة أسئلة قصيرة. لا تحتاج إلى حساب.");
        Add("Referee.WorkedYes",
            "Ja, dat klopt",
            "Yes, that is right",
            "Tak, to prawda",
            "Da, este adevărat",
            "نعم، هذا صحيح");
        Add("Referee.WorkedNo",
            "Nee, dat klopt niet",
            "No, that is not right",
            "Nie, to nieprawda",
            "Nu, nu este adevărat",
            "لا، هذا غير صحيح");
        Add("Referee.AgainYes",
            "Ja",
            "Yes",
            "Tak",
            "Da",
            "نعم");
        Add("Referee.AgainNo",
            "Nee",
            "No",
            "Nie",
            "Nu",
            "لا");
        Add("Referee.AgainMaybe",
            "Misschien",
            "Maybe",
            "Może",
            "Poate",
            "ربما");
    }
}
