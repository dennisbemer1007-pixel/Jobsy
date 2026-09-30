namespace Jobsy.Web.Localization;

/// <summary>Candidate test / deep-pay UI strings (nl/en now; pl/ro/ar = en until file 06).</summary>
public static class UiStringsTests
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = enText;
            ro[key] = enText;
            ar[key] = enText;
        }

        Add("DeepPay.Title", "Uitgebreide test", "Extended test");
        Add("DeepPay.What", "Uitgebreide test {0}", "Extended test {0}");
        Add("DeepPay.Questions", "{0} vragen", "{0} questions");
        Add("DeepPay.Get.1", "Uitgebreide uitslag met alle onderdelen", "Extended results for every part");
        Add("DeepPay.Get.2", "PDF-rapport om te bewaren", "PDF report to keep");
        Add("DeepPay.Get.3", "Tags voor betere matches", "Tags for better matches");
        Add("DeepPay.Get.4", "Je gratis uitslag blijft van jou", "Your free results stay yours");
        Add("DeepPay.PriceOnce", "Eenmalig · inclusief btw", "One-time · including VAT");
        Add("DeepPay.Methods", "Betaalmethoden", "Payment methods");
        Add("DeepPay.Waiver",
            "Ik wil meteen beginnen. Ik weet dat ik dan niet meer binnen 14 dagen kan annuleren.",
            "I want to start right away. I know I then cannot cancel within 14 days.");
        Add("DeepPay.FreeKeeps", "Je gratis uitslag blijft van jou.", "Your free results stay yours.");
        Add("DeepPay.NotNow", "Nu niet", "Not now");
        Add("DeepPay.PayCta", "Naar betalen · € {0}", "Pay · € {0}");
        Add("DeepPay.Terms", "Voorwaarden", "Terms");
        Add("DeepPay.SecureMollie", "Veilig betalen via Mollie", "Secure payment via Mollie");
        Add("DeepPay.StubHint", "Testbetaling: er wordt niets afgeschreven.", "Test payment: nothing will be charged.");
        Add("DeepPay.Err.waiver_required", "Zet eerst het vinkje.", "Tick the box first.");
        Add("DeepPay.Err.already_unlocked", "Je hebt deze test al. Begin meteen.", "You already have this test. Start now.");
        Add("DeepPay.Err.payments_unavailable", "Betalen lukt nu even niet. Probeer het later nog eens.", "Payment is unavailable right now. Try again later.");
        Add("DeepPay.Err.consent_required", "Je moet eerst toestemming geven voor de tests.", "You must consent to the tests first.");
        Add("DeepPay.Checking", "We checken je betaling", "We’re checking your payment");
        Add("DeepPay.CheckingLead", "Dit duurt meestal een paar seconden.", "This usually takes a few seconds.");
        Add("DeepPay.CheckingLong",
            "Duurt het langer dan een minuut? Je mag deze pagina sluiten. We sturen je een mail als het klaar is.",
            "Taking longer than a minute? You can close this page. We’ll email you when it’s ready.");
        Add("DeepPay.BackToTest", "Terug naar de test", "Back to the test");
        Add("DeepPay.PaidTitle", "Betaald. Je kunt beginnen", "Paid. You can start");
        Add("DeepPay.PaidMail", "Je krijgt de factuur ook per e-mail.", "You’ll also get the invoice by email.");
        Add("DeepPay.StartQ1", "Begin met vraag 1", "Start with question 1");
        Add("DeepPay.Later", "Later beginnen", "Start later");
        Add("DeepPay.FailedTitle", "De betaling is niet gelukt", "Payment failed");
        Add("DeepPay.FailedLead", "Er is niets afgeschreven.", "Nothing was charged.");
        Add("DeepPay.RetryPay", "Opnieuw betalen", "Try again");
        Add("DeepPay.StubFinish", "Testbetaling afronden", "Complete test payment");
        Add("DeepPay.NotFound", "We vinden deze betaling niet.", "We can’t find this payment.");
        Add("DeepPay.ToMyTests", "Naar Mijn tests", "Go to My tests");
        Add("DeepPay.Fact.What", "Wat", "What");
        Add("DeepPay.Fact.Amount", "Bedrag (incl. btw)", "Amount (incl. VAT)");
        Add("DeepPay.Fact.Date", "Datum", "Date");
        Add("DeepPay.Fact.Invoice", "Factuurnummer", "Invoice number");
        Add("TestErr.SaveBanner", "Je laatste antwoord is nog niet bewaard. Probeer het opnieuw.", "Your last answer is not saved yet. Please try again.");
        Add("TestErr.Retry", "Opnieuw proberen", "Try again");
        Add("TestErr.Consent", "Je moet eerst toestemming geven voor de tests.", "You must consent to the tests first.");
        Add("TestErr.unknown_question", "Die vraag kent deze test niet.", "That question is not part of this test.");
        Add("TestErr.invalid_answer", "Kies een antwoord van 1 tot 5.", "Choose an answer from 1 to 5.");
        Add("TestErr.consent_required", "Je moet eerst toestemming geven voor de tests.", "You must consent to the tests first.");
        Add("TestErr.parental_consent_required", "Je ouder of verzorger moet eerst toestemming geven.", "A parent or guardian must consent first.");
        Add("TestErr.Limit", "Je kunt deze test niet meer aanpassen.", "You cannot change this test any more.");
        Add("TestFlow.DraftBanner",
            "Je past je antwoorden aan. Pas als je op Afronden drukt, telt het als 1 van je 3 keer. Nog {0} over.",
            "You are editing answers. Only when you press Finish does it count as 1 of your 3 times. {0} left.");
        Add("TestFlow.DraftStop", "Stoppen zonder aanpassen", "Stop without changing");
        Add("TestFlow.QuotaZero",
            "Je hebt je antwoorden 3 keer aangepast. Dit is je uitslag.",
            "You have changed your answers 3 times. This is your result.");

        Add("TestDepth.First", "Eerste indruk", "First look");
        Add("TestDepth.Deeper", "Iets dieper", "A bit deeper");
        Add("TestDepth.Full", "Heel diep", "Very deep");
        Add("TestDepth.Bottom", "De bodem", "The bottom");

        Add("TestFlow.QuestionOf", "Vraag {0} van {1}", "Question {0} of {1}");
        Add("TestFlow.LevelDone", "{0} gedaan", "{0} done");
        Add("TestFlow.LevelNext", "nog {0} tot {1}", "{0} left until {1}");
        Add("TestFlow.Example", "Voorbeeld uit de praktijk", "Example from practice");
        Add("TestFlow.Answered", "Beantwoord ({0})", "Answered ({0})");
        Add("TestFlow.Adjust", "Aanpassen", "Adjust");
        Add("TestFlow.Back", "Terug", "Back");
        Add("TestFlow.Later", "Later verder", "Continue later");
        Add("TestFlow.Next", "Volgende", "Next");
        Add("TestFlow.Finish", "Afronden", "Finish");
        Add("TestFlow.Scale.Low", "Past niet", "Does not fit");
        Add("TestFlow.Scale.High", "Past heel goed", "Fits very well");
        Add("TestFlow.Scale.Aria.1", "1, past niet", "1, does not fit");
        Add("TestFlow.Scale.Aria.2", "2", "2");
        Add("TestFlow.Scale.Aria.3", "3", "3");
        Add("TestFlow.Scale.Aria.4", "4", "4");
        Add("TestFlow.Scale.Aria.5", "5, past heel goed", "5, fits very well");
        Add("TestFlow.Saved", "Bewaard", "Saved");
        Add("TestFlow.SaveFailed", "Je laatste antwoord is nog niet bewaard", "Your last answer is not saved yet");
        Add("TestFlow.Consent.Title", "Eerst even toestemming", "Consent first");
        Add("TestFlow.Consent.Lead", "Voor de tests gebruiken we je antwoorden om te kijken welk werk bij je past. Werkgevers zien je antwoorden niet.", "We use your answers to see which work fits you. Employers do not see your answers.");
        Add("TestFlow.Consent.Cta", "Toestemming geven", "Give consent");
        Add("TestFlow.Consent.Why", "Waarom vragen we dit?", "Why do we ask this?");
        Add("TestFlow.Consent.ParentalTitle", "Toestemming van je ouder", "Parent consent needed");
        Add("TestFlow.Consent.ParentalLead", "Je ouder of verzorger moet eerst toestemming geven.", "A parent or guardian must consent first.");
        Add("TestFlow.Consent.ParentalCta", "Naar toestemming", "Go to consent");
    }
}
