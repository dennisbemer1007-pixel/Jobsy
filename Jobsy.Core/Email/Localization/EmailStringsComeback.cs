namespace Jobsy.Core.Email.Localization;

/// <summary>Come-back reminder copy. Kept separate so locale catalogs stay easy to merge.</summary>
internal static class EmailStringsComeback
{
    public static IReadOnlyDictionary<string, string> For(string language) => language switch
    {
        "en" => En,
        "pl" => Pl,
        "ro" => Ro,
        "ar" => Ar,
        _ => Nl
    };

    private static readonly Dictionary<string, string> Nl = new(StringComparer.Ordinal)
    {
        ["Email.ComebackTests.Subject"] = "Je 4 korte tests staan nog open",
        ["Email.ComebackTests.Preheader"] = "Een herinnering van Lobsy",
        ["Email.ComebackTests.Heading"] = "Maak je tests af",
        ["Email.ComebackTests.P1"] = "Je bent een week geleden begonnen. De 4 korte tests zijn nog niet klaar. Ze helpen je om werk te vinden dat bij je past.",
        ["Email.ComebackTests.Cta"] = "Naar je tests",
        ["Email.ComebackTests.Eyebrow"] = "Even terug",
        ["Email.ComebackTests.PushTitle"] = "Je tests staan nog open",
        ["Email.ComebackTests.PushBody"] = "De 4 korte tests zijn nog niet klaar. Open Lobsy als je wilt doorgaan.",
        ["Email.ComebackTests.WhatsApp"] = "Hoi, dit is Lobsy. Je 4 korte tests staan nog open. Kijk op Lobsy als je wilt. Stoppen kan in je mail-instellingen.",
        ["Email.ComebackLookAgain.Subject"] = "Kijk nog eens, is er iets nieuws?",
        ["Email.ComebackLookAgain.Preheader"] = "Een herinnering van Lobsy",
        ["Email.ComebackLookAgain.Heading"] = "Er kan iets nieuws voor je zijn",
        ["Email.ComebackLookAgain.P1"] = "Je bent al 4 weken niet op Lobsy geweest. Kijk even of er iets nieuws voor je is.",
        ["Email.ComebackLookAgain.Cta"] = "Open Lobsy",
        ["Email.ComebackLookAgain.Eyebrow"] = "Even kijken",
        ["Email.ComebackLookAgain.PushTitle"] = "Kijk nog eens",
        ["Email.ComebackLookAgain.PushBody"] = "Je bent een tijd niet geweest. Kijk of er iets nieuws voor je is.",
        ["Email.ComebackLookAgain.WhatsApp"] = "Hoi, dit is Lobsy. Kijk nog eens, is er iets nieuws? Stoppen kan in je mail-instellingen.",
        ["Email.Reason.ComebackReminder"] = "Je vroeg zelf om deze herinnering."
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["Email.ComebackTests.Subject"] = "Your 4 short tests are still open",
        ["Email.ComebackTests.Preheader"] = "A reminder from Lobsy",
        ["Email.ComebackTests.Heading"] = "Finish your tests",
        ["Email.ComebackTests.P1"] = "You started a week ago. The 4 short tests are not finished yet. They help you find work that fits you.",
        ["Email.ComebackTests.Cta"] = "Go to your tests",
        ["Email.ComebackTests.Eyebrow"] = "A nudge",
        ["Email.ComebackTests.PushTitle"] = "Your tests are still open",
        ["Email.ComebackTests.PushBody"] = "The 4 short tests are not finished. Open Lobsy if you want to continue.",
        ["Email.ComebackTests.WhatsApp"] = "Hi, this is Lobsy. Your 4 short tests are still open. Open Lobsy if you want. You can stop this in your mail settings.",
        ["Email.ComebackLookAgain.Subject"] = "Take another look. Is there something new?",
        ["Email.ComebackLookAgain.Preheader"] = "A reminder from Lobsy",
        ["Email.ComebackLookAgain.Heading"] = "There may be something new for you",
        ["Email.ComebackLookAgain.P1"] = "You have not been on Lobsy for 4 weeks. Take a look and see if something new is there for you.",
        ["Email.ComebackLookAgain.Cta"] = "Go to Lobsy",
        ["Email.ComebackLookAgain.Eyebrow"] = "Look again",
        ["Email.ComebackLookAgain.PushTitle"] = "Take another look",
        ["Email.ComebackLookAgain.PushBody"] = "You have been away for a while. See if something new is there for you.",
        ["Email.ComebackLookAgain.WhatsApp"] = "Hi, this is Lobsy. Take another look. Is there something new? You can stop this in your mail settings.",
        ["Email.Reason.ComebackReminder"] = "You asked for this reminder yourself."
    };

    private static readonly Dictionary<string, string> Pl = new(StringComparer.Ordinal)
    {
        ["Email.ComebackTests.Subject"] = "Twoje 4 krótkie testy są jeszcze otwarte",
        ["Email.ComebackTests.Preheader"] = "Przypomnienie od Lobsy",
        ["Email.ComebackTests.Heading"] = "Dokończ testy",
        ["Email.ComebackTests.P1"] = "Zacząłeś tydzień temu. 4 krótkie testy nie są jeszcze gotowe. Pomagają znaleźć pracę, która do ciebie pasuje.",
        ["Email.ComebackTests.Cta"] = "Do twoich testów",
        ["Email.ComebackTests.Eyebrow"] = "Przypomnienie",
        ["Email.ComebackTests.PushTitle"] = "Testy są jeszcze otwarte",
        ["Email.ComebackTests.PushBody"] = "4 krótkie testy nie są gotowe. Otwórz Lobsy, jeśli chcesz iść dalej.",
        ["Email.ComebackTests.WhatsApp"] = "Cześć, tu Lobsy. Twoje 4 krótkie testy są jeszcze otwarte. Zajrzyj na Lobsy, jeśli chcesz. Możesz to wyłączyć w ustawieniach poczty.",
        ["Email.ComebackLookAgain.Subject"] = "Zajrzyj jeszcze raz. Czy jest coś nowego?",
        ["Email.ComebackLookAgain.Preheader"] = "Przypomnienie od Lobsy",
        ["Email.ComebackLookAgain.Heading"] = "Może jest coś nowego dla ciebie",
        ["Email.ComebackLookAgain.P1"] = "Nie było cię na Lobsy od 4 tygodni. Zajrzyj, czy jest coś nowego dla ciebie.",
        ["Email.ComebackLookAgain.Cta"] = "Otwórz Lobsy",
        ["Email.ComebackLookAgain.Eyebrow"] = "Zajrzyj",
        ["Email.ComebackLookAgain.PushTitle"] = "Zajrzyj jeszcze raz",
        ["Email.ComebackLookAgain.PushBody"] = "Dawno cię nie było. Zobacz, czy jest coś nowego dla ciebie.",
        ["Email.ComebackLookAgain.WhatsApp"] = "Cześć, tu Lobsy. Zajrzyj jeszcze raz, czy jest coś nowego? Możesz to wyłączyć w ustawieniach poczty.",
        ["Email.Reason.ComebackReminder"] = "Sam poprosiłeś o to przypomnienie."
    };

    private static readonly Dictionary<string, string> Ro = new(StringComparer.Ordinal)
    {
        ["Email.ComebackTests.Subject"] = "Cele 4 teste scurte sunt încă deschise",
        ["Email.ComebackTests.Preheader"] = "O reamintire de la Lobsy",
        ["Email.ComebackTests.Heading"] = "Termină testele",
        ["Email.ComebackTests.P1"] = "Ai început acum o săptămână. Cele 4 teste scurte nu sunt gata. Te ajută să găsești muncă potrivită pentru tine.",
        ["Email.ComebackTests.Cta"] = "La testele tale",
        ["Email.ComebackTests.Eyebrow"] = "O reamintire",
        ["Email.ComebackTests.PushTitle"] = "Testele sunt încă deschise",
        ["Email.ComebackTests.PushBody"] = "Cele 4 teste scurte nu sunt gata. Deschide Lobsy dacă vrei să continui.",
        ["Email.ComebackTests.WhatsApp"] = "Salut, aici e Lobsy. Cele 4 teste scurte sunt încă deschise. Intră pe Lobsy dacă vrei. Poți opri asta în setările de e-mail.",
        ["Email.ComebackLookAgain.Subject"] = "Mai aruncă o privire. Este ceva nou?",
        ["Email.ComebackLookAgain.Preheader"] = "O reamintire de la Lobsy",
        ["Email.ComebackLookAgain.Heading"] = "Poate este ceva nou pentru tine",
        ["Email.ComebackLookAgain.P1"] = "Nu ai fost pe Lobsy de 4 săptămâni. Uită-te dacă este ceva nou pentru tine.",
        ["Email.ComebackLookAgain.Cta"] = "Deschide Lobsy",
        ["Email.ComebackLookAgain.Eyebrow"] = "Mai uită-te",
        ["Email.ComebackLookAgain.PushTitle"] = "Mai aruncă o privire",
        ["Email.ComebackLookAgain.PushBody"] = "Ai lipsit o vreme. Vezi dacă este ceva nou pentru tine.",
        ["Email.ComebackLookAgain.WhatsApp"] = "Salut, aici e Lobsy. Mai aruncă o privire, este ceva nou? Poți opri asta în setările de e-mail.",
        ["Email.Reason.ComebackReminder"] = "Ai cerut tu această reamintire."
    };

    private static readonly Dictionary<string, string> Ar = new(StringComparer.Ordinal)
    {
        ["Email.ComebackTests.Subject"] = "اختباراتك الأربعة القصيرة ما زالت مفتوحة",
        ["Email.ComebackTests.Preheader"] = "تذكير من لوبسي",
        ["Email.ComebackTests.Heading"] = "أكمل اختباراتك",
        ["Email.ComebackTests.P1"] = "بدأت قبل أسبوع. الاختبارات الأربعة القصيرة لم تكتمل بعد. تساعدك على إيجاد عمل يناسبك.",
        ["Email.ComebackTests.Cta"] = "إلى اختباراتك",
        ["Email.ComebackTests.Eyebrow"] = "تذكير",
        ["Email.ComebackTests.PushTitle"] = "اختباراتك ما زالت مفتوحة",
        ["Email.ComebackTests.PushBody"] = "الاختبارات الأربعة القصيرة لم تكتمل. افتح لوبسي إذا أردت المتابعة.",
        ["Email.ComebackTests.WhatsApp"] = "مرحباً، هذا لوبسي. اختباراتك الأربعة القصيرة ما زالت مفتوحة. ادخل إلى لوبسي إذا أردت. يمكنك الإيقاف من إعدادات البريد.",
        ["Email.ComebackLookAgain.Subject"] = "ألقِ نظرة أخرى. هل هناك شيء جديد؟",
        ["Email.ComebackLookAgain.Preheader"] = "تذكير من لوبسي",
        ["Email.ComebackLookAgain.Heading"] = "قد يكون هناك شيء جديد لك",
        ["Email.ComebackLookAgain.P1"] = "لم تدخل إلى لوبسي منذ 4 أسابيع. انظر إن كان هناك شيء جديد لك.",
        ["Email.ComebackLookAgain.Cta"] = "افتح لوبسي",
        ["Email.ComebackLookAgain.Eyebrow"] = "انظر مجدداً",
        ["Email.ComebackLookAgain.PushTitle"] = "ألقِ نظرة أخرى",
        ["Email.ComebackLookAgain.PushBody"] = "غبت لفترة. انظر إن كان هناك شيء جديد لك.",
        ["Email.ComebackLookAgain.WhatsApp"] = "مرحباً، هذا لوبسي. ألقِ نظرة أخرى، هل هناك شيء جديد؟ يمكنك الإيقاف من إعدادات البريد.",
        ["Email.Reason.ComebackReminder"] = "أنت طلبت هذا التذكير بنفسك."
    };
}
