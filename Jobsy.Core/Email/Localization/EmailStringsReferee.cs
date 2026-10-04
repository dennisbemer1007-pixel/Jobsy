namespace Jobsy.Core.Email.Localization;

/// <summary>Referee confirmation mail. Separate so the big locale files stay untouched.</summary>
internal static class EmailStringsReferee
{
    public static IReadOnlyDictionary<string, string> Merge(IReadOnlyDictionary<string, string> source, string language)
    {
        var copy = new Dictionary<string, string>(source, StringComparer.Ordinal);
        foreach (var pair in For(language))
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static IReadOnlyDictionary<string, string> For(string language) => language switch
    {
        "en" => En,
        "pl" => Pl,
        "ro" => Ro,
        "ar" => Ar,
        _ => Nl
    };

    private static readonly Dictionary<string, string> Nl = new(StringComparer.Ordinal)
    {
        ["Email.ReferenceAsk.Subject"] = "{0} vraagt of je iets wilt bevestigen",
        ["Email.ReferenceAsk.Preheader"] = "5 korte vragen, geen account nodig",
        ["Email.ReferenceAsk.Heading"] = "Wil je dit bevestigen?",
        ["Email.ReferenceAsk.P1"] = "{0} zegt daar als {1} te hebben gewerkt. Klopt dat? Je hebt {2} dagen. Je hebt geen account nodig.",
        ["Email.ReferenceAsk.Cta"] = "Open de vragen",
        ["Email.ReferenceAsk.Eyebrow"] = "Een vraag",
        ["Email.ReferenceAsk.Note"] = "Liever niet? Op de pagina kun je nee zeggen of misbruik melden.",
        ["Email.ReferenceAsk.Privacy"] = "Lobsy vraagt dit omdat deze persoon jou noemde. Je antwoorden gaan naar die persoon. Die persoon kiest of een partner ze ziet. We bewaren dit zolang het account bestaat. Je hoeft niet te antwoorden.",
        ["Email.Reason.ReferenceAsked"] = "Iemand noemde jou als persoon die dit werk kan bevestigen.",
        ["Email.ReferenceConfirmed.PushTitle"] = "Je referent heeft geantwoord",
        ["Email.ReferenceConfirmed.PushBody"] = "{0} heeft je werk bevestigd. Je ziet de antwoorden in je paspoort.",
        ["Email.ReferenceDeclined.PushTitle"] = "Je referent doet niet mee",
        ["Email.ReferenceDeclined.PushBody"] = "{0} wil de vragen niet invullen."
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["Email.ReferenceAsk.Subject"] = "{0} asks you to confirm something",
        ["Email.ReferenceAsk.Preheader"] = "5 short questions, no account needed",
        ["Email.ReferenceAsk.Heading"] = "Will you confirm this?",
        ["Email.ReferenceAsk.P1"] = "{0} says they worked there as {1}. Is that right? You have {2} days. You do not need an account.",
        ["Email.ReferenceAsk.Cta"] = "Open the questions",
        ["Email.ReferenceAsk.Eyebrow"] = "A question",
        ["Email.ReferenceAsk.Note"] = "Rather not? On the page you can say no or report misuse.",
        ["Email.ReferenceAsk.Privacy"] = "Lobsy asks this because this person named you. Your answers go to that person. That person chooses whether a partner sees them. We keep this while the account exists. You do not have to answer.",
        ["Email.Reason.ReferenceAsked"] = "Someone named you as a person who can confirm this work.",
        ["Email.ReferenceConfirmed.PushTitle"] = "Your referee answered",
        ["Email.ReferenceConfirmed.PushBody"] = "{0} confirmed your work. You can see the answers in your passport.",
        ["Email.ReferenceDeclined.PushTitle"] = "Your referee will not take part",
        ["Email.ReferenceDeclined.PushBody"] = "{0} does not want to answer the questions."
    };

    private static readonly Dictionary<string, string> Pl = new(StringComparer.Ordinal)
    {
        ["Email.ReferenceAsk.Subject"] = "{0} prosi, żeby coś potwierdzić",
        ["Email.ReferenceAsk.Preheader"] = "5 krótkich pytań, bez konta",
        ["Email.ReferenceAsk.Heading"] = "Chcesz to potwierdzić?",
        ["Email.ReferenceAsk.P1"] = "{0} mówi, że pracował tam jako {1}. Czy to prawda? Masz {2} dni. Konto nie jest potrzebne.",
        ["Email.ReferenceAsk.Cta"] = "Otwórz pytania",
        ["Email.ReferenceAsk.Eyebrow"] = "Pytanie",
        ["Email.ReferenceAsk.Note"] = "Wolisz nie? Na stronie możesz odmówić albo zgłosić nadużycie.",
        ["Email.ReferenceAsk.Privacy"] = "Lobsy pyta, bo ta osoba cię wskazała. Odpowiedzi idą do niej. Ta osoba wybiera, czy partner je widzi. Trzymamy to, dopóki konto istnieje. Nie musisz odpowiadać.",
        ["Email.Reason.ReferenceAsked"] = "Ktoś podał ciebie jako osobę, która może potwierdzić tę pracę.",
        ["Email.ReferenceConfirmed.PushTitle"] = "Twoja osoba odpowiedziała",
        ["Email.ReferenceConfirmed.PushBody"] = "{0} potwierdził twoją pracę. Odpowiedzi widzisz w paszporcie.",
        ["Email.ReferenceDeclined.PushTitle"] = "Twoja osoba nie bierze udziału",
        ["Email.ReferenceDeclined.PushBody"] = "{0} nie chce odpowiedzieć na pytania."
    };

    private static readonly Dictionary<string, string> Ro = new(StringComparer.Ordinal)
    {
        ["Email.ReferenceAsk.Subject"] = "{0} te roagă să confirmi ceva",
        ["Email.ReferenceAsk.Preheader"] = "5 întrebări scurte, fără cont",
        ["Email.ReferenceAsk.Heading"] = "Vrei să confirmi asta?",
        ["Email.ReferenceAsk.P1"] = "{0} spune că a lucrat acolo ca {1}. Este adevărat? Ai {2} zile. Nu ai nevoie de cont.",
        ["Email.ReferenceAsk.Cta"] = "Deschide întrebările",
        ["Email.ReferenceAsk.Eyebrow"] = "O întrebare",
        ["Email.ReferenceAsk.Note"] = "Mai bine nu? Pe pagină poți spune nu sau poți semnala un abuz.",
        ["Email.ReferenceAsk.Privacy"] = "Lobsy întreabă pentru că această persoană te-a numit. Răspunsurile merg la ea. Ea alege dacă un partener le vede. Le păstrăm cât există contul. Nu trebuie să răspunzi.",
        ["Email.Reason.ReferenceAsked"] = "Cineva te-a numit ca persoană care poate confirma această muncă.",
        ["Email.ReferenceConfirmed.PushTitle"] = "Persoana ta a răspuns",
        ["Email.ReferenceConfirmed.PushBody"] = "{0} a confirmat munca ta. Vezi răspunsurile în pașaport.",
        ["Email.ReferenceDeclined.PushTitle"] = "Persoana ta nu participă",
        ["Email.ReferenceDeclined.PushBody"] = "{0} nu vrea să răspundă la întrebări."
    };

    private static readonly Dictionary<string, string> Ar = new(StringComparer.Ordinal)
    {
        ["Email.ReferenceAsk.Subject"] = "{0} يطلب منك تأكيد شيء",
        ["Email.ReferenceAsk.Preheader"] = "5 أسئلة قصيرة، بلا حساب",
        ["Email.ReferenceAsk.Heading"] = "هل تؤكد هذا؟",
        ["Email.ReferenceAsk.P1"] = "{0} يقول إنه عمل هناك بصفة {1}. هل هذا صحيح؟ لديك {2} يوماً. لا تحتاج إلى حساب.",
        ["Email.ReferenceAsk.Cta"] = "افتح الأسئلة",
        ["Email.ReferenceAsk.Eyebrow"] = "سؤال",
        ["Email.ReferenceAsk.Note"] = "لا تريد؟ في الصفحة يمكنك الرفض أو الإبلاغ عن إساءة.",
        ["Email.ReferenceAsk.Privacy"] = "يسأل لوبسي لأن هذا الشخص سمّاك. إجاباتك تذهب إليه. هو يختار إن كان الشريك يراها. نحتفظ بها ما دام الحساب موجوداً. لست مضطراً للإجابة.",
        ["Email.Reason.ReferenceAsked"] = "سمّاك أحدهم شخصاً يستطيع تأكيد هذا العمل.",
        ["Email.ReferenceConfirmed.PushTitle"] = "أجاب مرجِعك",
        ["Email.ReferenceConfirmed.PushBody"] = "{0} أكد عملك. ترى الإجابات في جوازك.",
        ["Email.ReferenceDeclined.PushTitle"] = "مرجِعك لا يشارك",
        ["Email.ReferenceDeclined.PushBody"] = "{0} لا يريد الإجابة عن الأسئلة."
    };
}
