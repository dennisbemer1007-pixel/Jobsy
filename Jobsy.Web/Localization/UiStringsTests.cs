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


        Add("TestFlow.Rail.Aria", "Jouw duik", "Your dive");
        Add("TestFlow.Eyebrow.Intro", "Mijn tests · {0}", "My tests · {0}");
        Add("TestFlow.Eyebrow.Dive", "In de diepte · {0} · {1}", "Diving · {0} · {1}");
        Add("TestFlow.QuestionLead", "Hoe goed past deze zin bij jou? Er zijn geen foute antwoorden.", "How well does this sentence fit you? There are no wrong answers.");
        Add("TestFlow.Lead.Competence", "25 korte zinnen over hoe je werkt. Zo zien we waar je sterk in bent.", "25 short sentences about how you work. We see where you are strong.");
        Add("TestFlow.Lead.Career", "25 korte zinnen over werk dat je leuk vindt. Zo vinden we beroepen die bij je passen.", "25 short sentences about work you like. We find careers that fit.");
        Add("TestFlow.Lead.Culture", "18 korte zinnen over waar je graag werkt. Zo vinden we werkplekken waar je je thuis voelt.", "18 short sentences about where you like to work.");
        Add("TestFlow.Lead.Values", "25 korte zinnen over wat je belangrijk vindt in werk.", "25 short sentences about what matters to you at work.");
        Add("TestFlow.DepthPick", "Hoe diep wil je duiken?", "How deep do you want to dive?");
        Add("TestFlow.DepthMeta", "{0} vragen · + {1} min", "{0} questions · + {1} min");
        Add("TestFlow.ToMyTests", "Mijn tests", "My tests");
        Add("TestFlow.Begin", "Begin", "Start");
        Add("TestFlow.ContinueAt", "Ga verder bij vraag {0}", "Continue at question {0}");
        Add("TestFlow.Bubble.IntroStart", "Klaar voor een duik? Ik ga met je mee.", "Ready for a dive? I’ll come with you.");
        Add("TestFlow.Bubble.IntroContinue", "Vijf vragen heb je al gedaan. Zullen we samen wat dieper gaan?", "You’ve already done five questions. Shall we go a bit deeper?");
        Add("TestFlow.Bubble.Question", "Er zijn geen foute antwoorden. Kies wat het eerst in je opkomt.", "There are no wrong answers. Choose what comes first.");
        Add("TestDone.Title", "Weer een laag eraf", "Another layer off");
        Add("TestDone.Eyebrow", "In de diepte · {0} · {1} klaar", "Diving · {0} · {1} done");
        Add("TestDone.LeadFull", "{0} is helemaal klaar. Dit staat nu in je paspoort:", "{0} is fully done. This is now in your passport:");
        Add("TestDone.LeadLevel", "{0}: {1} is klaar. Dit staat nu in je paspoort:", "{0}: {1} is done. This is now in your passport:");
        Add("TestDone.Live", "Weer een laag eraf. {0} {1} is klaar.", "Another layer off. {0} {1} is done.");
        Add("TestDone.Back", "Terug naar Mijn tests", "Back to My tests");
        Add("TestDone.ViewResult", "Bekijk je uitslag", "View your result");
        Add("TestDone.BackToJourney", "Terug naar de reis", "Back to the journey");
        Add("TestDone.Bubble.Competence", "Voel je dat? Weer een laag eraf. Nu zie ik nog beter hoe je werkt.", "Feel that? Another layer off. I see better how you work.");
        Add("TestDone.Bubble.Career", "Voel je dat? Weer een laag eraf. Nu zie ik nog beter wat je leuk vindt.", "Feel that? Another layer off. I see better what you like.");
        Add("TestDone.Bubble.Culture", "Voel je dat? Weer een laag eraf. Nu zie ik nog beter waar je je thuis voelt.", "Feel that? Another layer off. I see better where you feel at home.");
        Add("TestDone.Bubble.Values", "Voel je dat? Weer een laag eraf. Nu zie ik nog beter wat je belangrijk vindt.", "Feel that? Another layer off. I see better what matters to you.");
        Add("TestDone.Gain.Strength", "Je sterke kant komt duidelijker naar voren", "Your strength comes through more clearly");
        Add("TestDone.Gain.Strength2", "We zien beter waar je energie van krijgt", "We see better what gives you energy");
        Add("TestDone.Gain.Less", "En wat minder jouw ding is", "And what is less your thing");
        Add("TestDone.CareerMatches", "{0} vacatures passen goed bij wat je leuk vindt", "{0} vacancies fit well with what you like");
        Add("TestDone.CareerMatchesLink", "Bekijk ze", "View them");

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

        Add("DeepPay.Upsell",
            "Ontdek meer over jezelf met de uitgebreide test: {0} vragen en een rapport.",
            "Discover more about yourself with the extended test: {0} questions and a report.");
        Add("Deep.UnknownTitle", "Deze test bestaat niet.", "This test does not exist.");
        Add("Deep.UnknownLead", "Kies een test via Mijn tests.", "Pick a test from My tests.");
        Add("Deep.Eyebrow.Part", "De bodem · {0} · onderwerp {1} van 5", "The bottom · {0} · topic {1} of 5");
        Add("Deep.TopicsLabel", "Onderwerpen", "Topics");
        Add("Deep.Bubble.Question", "Hier beneden is het stil. Neem je tijd, we gaan in stukjes.", "It’s quiet down here. Take your time; we go in pieces.");
        Add("Deep.Motivation.PartStart", "Onderwerp {0} van 5: {1}.", "Topic {0} of 5: {1}.");
        Add("Deep.Motivation.Halfway", "Je bent halverwege. Goed bezig.", "You’re halfway. Well done.");
        Add("Deep.Motivation.LastPart", "Laatste onderwerp. Bijna op de bodem.", "Last topic. Almost at the bottom.");
        Add("Deep.Motivation.BeforePause", "Na dit onderwerp komt een rustpunt. Stoppen mag: alles is bewaard.", "After this topic comes a pause. Stopping is fine: everything is saved.");
        Add("Deep.Pause.Title", "Rustpunt", "Pause point");
        Add("Deep.Pause.Lead", "Onderwerp {0} is klaar. Je hebt {1} van {2} vragen gedaan.", "Topic {0} is done. You’ve answered {1} of {2} questions.");
        Add("Deep.Pause.Live", "Rustpunt. Onderwerp {0} is klaar.", "Pause point. Topic {0} is done.");
        Add("Deep.Pause.Continue", "Verder met onderwerp {0}", "Continue with topic {0}");
        Add("Deep.Pause.Bubble", "Even ademhalen mag. Ik blijf hier.", "A breath is fine. I’ll stay here.");
        Add("Deep.Done.Eyebrow", "De bodem · {0} van {1}", "The bottom · {0} of {1}");
        Add("Deep.Done.Lead", "Je bent op de bodem. Je rapport is klaar.", "You’re at the bottom. Your report is ready.");
        Add("Deep.Done.ViewReport", "Bekijk je rapport", "View your report");
        Add("Deep.Done.DownloadPdf", "Download als PDF", "Download as PDF");
        Add("Deep.Done.Bubble", "Voel je dat? Nu ken ik je echt.", "Feel that? Now I really know you.");
        Add("Deep.Offer.Eyebrow", "De bodem · uitgebreide test", "The bottom · extended test");
        Add("Deep.Offer.Title", "Duik tot de bodem", "Dive to the bottom");
        Add("Deep.Offer.Lead", "{0} vragen over {1}. Daarna krijg je een rapport, helemaal over jou.", "{0} questions about {1}. Then you get a report, all about you.");
        Add("Deep.Offer.WhatTitle", "Wat krijg je?", "What do you get?");
        Add("Deep.Offer.Sample", "Bekijk eerst een voorbeeld", "View a sample first");
        Add("Deep.Offer.TileMin", "± {0} minuten", "± {0} minutes");
        Add("Deep.Offer.TileParts", "In stukjes. Alles wordt bewaard.", "In pieces. Everything is saved.");
        Add("Deep.Offer.TileFreeKeep", "Je gratis uitslag blijft", "Your free result stays");
        Add("Deep.Offer.TileFreeKeepLead", "Die {0} vragen blijven altijd van jou.", "Those {0} questions stay yours forever.");
        Add("Deep.Offer.Bubble", "Daar beneden ligt nog meer van jou. Kijken mag, het hoeft niet.", "There’s more of you down there. Looking is fine; it doesn’t have to.");
        Add("Deep.Rail.FreeDone", "Heel diep · {0} vragen · gedaan", "Very deep · {0} questions · done");
        Add("Deep.Rail.BottomZone", "De bodem · uitgebreid", "The bottom · extended");
        Add("Deep.Rail.PaidReady", "Betaald · klaar om te duiken", "Paid · ready to dive");
        Add("Deep.Rail.PartProgress", "Vraag {0} van {1}", "Question {0} of {1}");
        Add("Deep.Rail.Footer", "{0} van {1} vragen · ± {2} min nog", "{0} of {1} questions · ± {2} min left");
        Add("Deep.Checkout.Bubble.Checking", "Even geduld. We kijken of de betaling gelukt is.", "One moment. We’re checking the payment.");
        Add("Deep.Checkout.Bubble.Paid", "Gelukt! Zullen we samen naar de bodem duiken?", "Done! Shall we dive to the bottom together?");
        Add("Deep.Checkout.Bubble.Failed", "Geen zorgen. Er is niets afgeschreven.", "No worries. Nothing was charged.");
        Add("Deep.Checkout.NoDouble", "Je betaalt nooit twee keer voor dezelfde test.", "You never pay twice for the same test.");
        Add("Deep.Checkout.PaidLead", "Fijn! De uitgebreide {0}test staat voor je klaar.", "Nice! The extended {0} test is ready for you.");
        Add("Deep.Checkout.FailedLead",
            "Er is niets afgeschreven. Dat kan gebeuren, bijvoorbeeld als je het scherm van je bank sloot.",
            "Nothing was charged. That can happen if you closed your bank screen.");
        Add("Deep.Checkout.TipRetry", "Je kunt het opnieuw proberen.", "You can try again.");
        Add("Deep.Checkout.TipKept", "Je eerdere antwoorden blijven bewaard.", "Your earlier answers stay saved.");
        Add("Deep.Checkout.TipHelp", "Is er toch geld afgeschreven? Tik op Assistent. Dan zoeken we het uit.", "Was money charged anyway? Tap Assistant and we’ll look into it.");
        Add("Deep.Checkout.TileQs", "{0} vragen · ± {1} min", "{0} questions · ± {1} min");
        Add("Deep.Checkout.TileParts", "In 5 onderwerpen, met rustpunten.", "In 5 topics, with pause points.");
        Add("Deep.Checkout.TileStop", "Stoppen mag", "Stopping is fine");
        Add("Deep.Checkout.TileStopLead", "Je gaat later verder waar je was.", "You’ll continue later where you left off.");
    }
}
