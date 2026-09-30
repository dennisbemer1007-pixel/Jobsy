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
    }
}
