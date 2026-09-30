using Jobsy.Core.Rules;

namespace Jobsy.Web.Localization;

public static class UiStringsGratisDna
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        AddUi(nl, en, pl, ro, ar);

        AddTile(nl, en, pl, ro, ar, "Strength", CompetencyTestCatalog.Samenwerken,
            OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Samenwerken),
            "You like working with others and take them into account.");
        AddTile(nl, en, pl, ro, ar, "Strength", CompetencyTestCatalog.Resultaatgerichtheid,
            OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Resultaatgerichtheid),
            "You like finishing things and clear results.");
        AddTile(nl, en, pl, ro, ar, "Strength", CompetencyTestCatalog.Stressbestendigheid,
            OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Stressbestendigheid),
            "You stay relatively calm when it gets busy.");
        AddTile(nl, en, pl, ro, ar, "Strength", CompetencyTestCatalog.Innovatie,
            OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Innovatie),
            "You are open to new ideas and ways of working.");
        AddTile(nl, en, pl, ro, ar, "Strength", CompetencyTestCatalog.Extraversie,
            OnboardingImpressionLibrary.StrengthSentence(CompetencyTestCatalog.Extraversie),
            "You get energy from contact with people.");

        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Realistic,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Realistic),
            "You fit practical work with your hands or machines.");
        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Investigative,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Investigative),
            "You fit work where you investigate and understand things.");
        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Artistic,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Artistic),
            "You fit work with creativity and your own input.");
        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Social,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Social),
            "You fit work where you help or guide others.");
        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Enterprising,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Enterprising),
            "You fit work with persuading, selling or leading.");
        AddTile(nl, en, pl, ro, ar, "Riasec", CareerTestCatalog.Conventional,
            OnboardingImpressionLibrary.RiasecSentence(CareerTestCatalog.Conventional),
            "You fit work with order, numbers or administration.");

        foreach (var code in OnboardingWizardCatalog.CultureDimensionCodes.Concat([CulturePersonalityCatalog.Innovation]))
        {
            AddTile(nl, en, pl, ro, ar, "Culture", code,
                OnboardingImpressionLibrary.CultureSentence(code),
                OnboardingImpressionLibrary.CultureSentence(code));
        }

        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            AddTile(nl, en, pl, ro, ar, "Value", code,
                OnboardingImpressionLibrary.ValueSentence(code),
                OnboardingImpressionLibrary.ValueSentence(code));
        }
    }

    private static void AddTile(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar,
        string group, string code, string nlSentence, string enSentence)
    {
        var key = $"GratisDna.Tile.{group}.{code}";
        nl[key] = nlSentence;
        en[key] = enSentence;
        pl[key] = enSentence;
        ro[key] = enSentence;
        ar[key] = enSentence;
    }

    private static void AddUi(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        nl["GratisDna.Seo.Title"] = "Ontdek je werk-DNA";
        en["GratisDna.Seo.Title"] = "Discover your work DNA";
        pl["GratisDna.Seo.Title"] = en["GratisDna.Seo.Title"];
        ro["GratisDna.Seo.Title"] = en["GratisDna.Seo.Title"];
        ar["GratisDna.Seo.Title"] = en["GratisDna.Seo.Title"];

        nl["GratisDna.Seo.Description"] = "20 korte vragen, geen account. Zie meteen hoe jij werkt en wat bij je past.";
        en["GratisDna.Seo.Description"] = "20 short questions, no account. See right away how you work and what fits you.";
        pl["GratisDna.Seo.Description"] = en["GratisDna.Seo.Description"];
        ro["GratisDna.Seo.Description"] = en["GratisDna.Seo.Description"];
        ar["GratisDna.Seo.Description"] = en["GratisDna.Seo.Description"];

        nl["GratisDna.Landing.Badge"] = "Gratis · geen account nodig";
        en["GratisDna.Landing.Badge"] = "Free · no account needed";
        pl["GratisDna.Landing.Badge"] = en["GratisDna.Landing.Badge"];
        ro["GratisDna.Landing.Badge"] = en["GratisDna.Landing.Badge"];
        ar["GratisDna.Landing.Badge"] = en["GratisDna.Landing.Badge"];

        nl["GratisDna.Landing.Title"] = "Ontdek je werk-DNA";
        en["GratisDna.Landing.Title"] = "Discover your work DNA";
        pl["GratisDna.Landing.Title"] = en["GratisDna.Landing.Title"];
        ro["GratisDna.Landing.Title"] = en["GratisDna.Landing.Title"];
        ar["GratisDna.Landing.Title"] = en["GratisDna.Landing.Title"];

        nl["GratisDna.Landing.Lead"] = "20 korte vragen over jou. Er is geen goed of fout: kies gewoon wat je voelt. Na 3 minuutjes zie je: dit ben jij.";
        en["GratisDna.Landing.Lead"] = "20 short questions about you. There is no right or wrong: just choose what you feel. After about 3 minutes you see: this is you.";
        pl["GratisDna.Landing.Lead"] = en["GratisDna.Landing.Lead"];
        ro["GratisDna.Landing.Lead"] = en["GratisDna.Landing.Lead"];
        ar["GratisDna.Landing.Lead"] = en["GratisDna.Landing.Lead"];

        nl["GratisDna.Landing.OwnLanguage"] = "Liever in je eigen taal? Kies je taal ↗";
        en["GratisDna.Landing.OwnLanguage"] = "Prefer your own language? Choose your language ↗";
        pl["GratisDna.Landing.OwnLanguage"] = en["GratisDna.Landing.OwnLanguage"];
        ro["GratisDna.Landing.OwnLanguage"] = en["GratisDna.Landing.OwnLanguage"];
        ar["GratisDna.Landing.OwnLanguage"] = en["GratisDna.Landing.OwnLanguage"];

        nl["GratisDna.Landing.MetaMinutes"] = "± 3 minuten";
        en["GratisDna.Landing.MetaMinutes"] = "± 3 minutes";
        pl["GratisDna.Landing.MetaMinutes"] = en["GratisDna.Landing.MetaMinutes"];
        ro["GratisDna.Landing.MetaMinutes"] = en["GratisDna.Landing.MetaMinutes"];
        ar["GratisDna.Landing.MetaMinutes"] = en["GratisDna.Landing.MetaMinutes"];

        nl["GratisDna.Landing.MetaQuestions"] = "{0} vragen";
        en["GratisDna.Landing.MetaQuestions"] = "{0} questions";
        pl["GratisDna.Landing.MetaQuestions"] = en["GratisDna.Landing.MetaQuestions"];
        ro["GratisDna.Landing.MetaQuestions"] = en["GratisDna.Landing.MetaQuestions"];
        ar["GratisDna.Landing.MetaQuestions"] = en["GratisDna.Landing.MetaQuestions"];

        nl["GratisDna.Landing.MetaDevice"] = "Blijft op dit apparaat";
        en["GratisDna.Landing.MetaDevice"] = "Stays on this device";
        pl["GratisDna.Landing.MetaDevice"] = en["GratisDna.Landing.MetaDevice"];
        ro["GratisDna.Landing.MetaDevice"] = en["GratisDna.Landing.MetaDevice"];
        ar["GratisDna.Landing.MetaDevice"] = en["GratisDna.Landing.MetaDevice"];

        nl["GratisDna.Landing.Block.Strength"] = "Samenwerken, doorzetten, rust als het druk is · 5 vragen";
        en["GratisDna.Landing.Block.Strength"] = "Working together, finishing, calm when it is busy · 5 questions";
        pl["GratisDna.Landing.Block.Strength"] = en["GratisDna.Landing.Block.Strength"];
        ro["GratisDna.Landing.Block.Strength"] = en["GratisDna.Landing.Block.Strength"];
        ar["GratisDna.Landing.Block.Strength"] = en["GratisDna.Landing.Block.Strength"];

        nl["GratisDna.Landing.Block.Riasec"] = "Waar je energie van krijgt · 5 vragen";
        en["GratisDna.Landing.Block.Riasec"] = "What gives you energy · 5 questions";
        pl["GratisDna.Landing.Block.Riasec"] = en["GratisDna.Landing.Block.Riasec"];
        ro["GratisDna.Landing.Block.Riasec"] = en["GratisDna.Landing.Block.Riasec"];
        ar["GratisDna.Landing.Block.Riasec"] = en["GratisDna.Landing.Block.Riasec"];

        nl["GratisDna.Landing.Block.Culture"] = "Wat voor team en plek bij je past · 5 vragen";
        en["GratisDna.Landing.Block.Culture"] = "What kind of team and place fits you · 5 questions";
        pl["GratisDna.Landing.Block.Culture"] = en["GratisDna.Landing.Block.Culture"];
        ro["GratisDna.Landing.Block.Culture"] = en["GratisDna.Landing.Block.Culture"];
        ar["GratisDna.Landing.Block.Culture"] = en["GratisDna.Landing.Block.Culture"];

        nl["GratisDna.Landing.Block.Value"] = "Wat jou drijft · 5 vragen";
        en["GratisDna.Landing.Block.Value"] = "What drives you · 5 questions";
        pl["GratisDna.Landing.Block.Value"] = en["GratisDna.Landing.Block.Value"];
        ro["GratisDna.Landing.Block.Value"] = en["GratisDna.Landing.Block.Value"];
        ar["GratisDna.Landing.Block.Value"] = en["GratisDna.Landing.Block.Value"];

        nl["GratisDna.Landing.Bubble"] = "Duik maar diep! 🌊 Je kunt altijd terug.";
        en["GratisDna.Landing.Bubble"] = "Dive right in! 🌊 You can always go back.";
        pl["GratisDna.Landing.Bubble"] = en["GratisDna.Landing.Bubble"];
        ro["GratisDna.Landing.Bubble"] = en["GratisDna.Landing.Bubble"];
        ar["GratisDna.Landing.Bubble"] = en["GratisDna.Landing.Bubble"];

        nl["GratisDna.Landing.AfterTitle"] = "Wat krijg je na de test?";
        en["GratisDna.Landing.AfterTitle"] = "What do you get after the test?";
        pl["GratisDna.Landing.AfterTitle"] = en["GratisDna.Landing.AfterTitle"];
        ro["GratisDna.Landing.AfterTitle"] = en["GratisDna.Landing.AfterTitle"];
        ar["GratisDna.Landing.AfterTitle"] = en["GratisDna.Landing.AfterTitle"];

        nl["GratisDna.Landing.AfterLead"] = "Een eerste “Dit ben jij”: hoe je werkt, wat je leuk vindt, waar je je thuis voelt en wat je belangrijk vindt.";
        en["GratisDna.Landing.AfterLead"] = "A first “This is you”: how you work, what you enjoy, where you feel at home and what matters to you.";
        pl["GratisDna.Landing.AfterLead"] = en["GratisDna.Landing.AfterLead"];
        ro["GratisDna.Landing.AfterLead"] = en["GratisDna.Landing.AfterLead"];
        ar["GratisDna.Landing.AfterLead"] = en["GratisDna.Landing.AfterLead"];

        nl["GratisDna.Landing.PrivacyLink"] = "Meer over privacy";
        en["GratisDna.Landing.PrivacyLink"] = "More about privacy";
        pl["GratisDna.Landing.PrivacyLink"] = en["GratisDna.Landing.PrivacyLink"];
        ro["GratisDna.Landing.PrivacyLink"] = en["GratisDna.Landing.PrivacyLink"];
        ar["GratisDna.Landing.PrivacyLink"] = en["GratisDna.Landing.PrivacyLink"];

        nl["GratisDna.Landing.PreviewTitle"] = "Zo ziet jouw DNA eruit";
        en["GratisDna.Landing.PreviewTitle"] = "This is what your DNA looks like";
        pl["GratisDna.Landing.PreviewTitle"] = en["GratisDna.Landing.PreviewTitle"];
        ro["GratisDna.Landing.PreviewTitle"] = en["GratisDna.Landing.PreviewTitle"];
        ar["GratisDna.Landing.PreviewTitle"] = en["GratisDna.Landing.PreviewTitle"];

        nl["GratisDna.Landing.Step1"] = "Beantwoord 20 stellingen · 1 = past niet, 5 = past wel";
        en["GratisDna.Landing.Step1"] = "Answer 20 statements · 1 = does not fit, 5 = fits well";
        pl["GratisDna.Landing.Step1"] = en["GratisDna.Landing.Step1"];
        ro["GratisDna.Landing.Step1"] = en["GratisDna.Landing.Step1"];
        ar["GratisDna.Landing.Step1"] = en["GratisDna.Landing.Step1"];

        nl["GratisDna.Landing.Step2"] = "Zie je werk-DNA · in gewone taal, zonder account";
        en["GratisDna.Landing.Step2"] = "See your work DNA · in plain language, no account";
        pl["GratisDna.Landing.Step2"] = en["GratisDna.Landing.Step2"];
        ro["GratisDna.Landing.Step2"] = en["GratisDna.Landing.Step2"];
        ar["GratisDna.Landing.Step2"] = en["GratisDna.Landing.Step2"];

        nl["GratisDna.Landing.Step3"] = "Deel het of bewaar het · met een gratis account zie je banen die passen";
        en["GratisDna.Landing.Step3"] = "Share or save it · with a free account you see jobs that fit";
        pl["GratisDna.Landing.Step3"] = en["GratisDna.Landing.Step3"];
        ro["GratisDna.Landing.Step3"] = en["GratisDna.Landing.Step3"];
        ar["GratisDna.Landing.Step3"] = en["GratisDna.Landing.Step3"];

        nl["GratisDna.Landing.Age16"] = "Ik ben 16 jaar of ouder.";
        en["GratisDna.Landing.Age16"] = "I am 16 or older.";
        pl["GratisDna.Landing.Age16"] = en["GratisDna.Landing.Age16"];
        ro["GratisDna.Landing.Age16"] = en["GratisDna.Landing.Age16"];
        ar["GratisDna.Landing.Age16"] = en["GratisDna.Landing.Age16"];

        nl["GratisDna.Landing.AgeUnder16"] = "Jonger dan 16?";
        en["GratisDna.Landing.AgeUnder16"] = "Younger than 16?";
        pl["GratisDna.Landing.AgeUnder16"] = en["GratisDna.Landing.AgeUnder16"];
        ro["GratisDna.Landing.AgeUnder16"] = en["GratisDna.Landing.AgeUnder16"];
        ar["GratisDna.Landing.AgeUnder16"] = en["GratisDna.Landing.AgeUnder16"];

        nl["GratisDna.Landing.Consent"] = "Ik snap dat mijn antwoorden 7 dagen op dit apparaat blijven. Werkgevers zien ze nooit.";
        en["GratisDna.Landing.Consent"] = "I understand my answers stay on this device for 7 days. Employers never see them.";
        pl["GratisDna.Landing.Consent"] = en["GratisDna.Landing.Consent"];
        ro["GratisDna.Landing.Consent"] = en["GratisDna.Landing.Consent"];
        ar["GratisDna.Landing.Consent"] = en["GratisDna.Landing.Consent"];

        nl["GratisDna.Landing.StartCta"] = "Start de test";
        en["GratisDna.Landing.StartCta"] = "Start the test";
        pl["GratisDna.Landing.StartCta"] = en["GratisDna.Landing.StartCta"];
        ro["GratisDna.Landing.StartCta"] = en["GratisDna.Landing.StartCta"];
        ar["GratisDna.Landing.StartCta"] = en["GratisDna.Landing.StartCta"];

        nl["GratisDna.Landing.Footer"] = "Je antwoorden blijven op dit apparaat tot je een account maakt. Werkgevers zien ze nooit.";
        en["GratisDna.Landing.Footer"] = "Your answers stay on this device until you create an account. Employers never see them.";
        pl["GratisDna.Landing.Footer"] = en["GratisDna.Landing.Footer"];
        ro["GratisDna.Landing.Footer"] = en["GratisDna.Landing.Footer"];
        ar["GratisDna.Landing.Footer"] = en["GratisDna.Landing.Footer"];

        nl["GratisDna.Landing.Resume"] = "Verder waar je was";
        en["GratisDna.Landing.Resume"] = "Continue where you left off";
        pl["GratisDna.Landing.Resume"] = en["GratisDna.Landing.Resume"];
        ro["GratisDna.Landing.Resume"] = en["GratisDna.Landing.Resume"];
        ar["GratisDna.Landing.Resume"] = en["GratisDna.Landing.Resume"];

        nl["GratisDna.Landing.EmployerNote"] = "Deze test is bedoeld voor werkzoekenden. Als werkgever kun je de test bekijken, maar we slaan geen antwoorden op.";
        en["GratisDna.Landing.EmployerNote"] = "This test is for job seekers. As an employer you can try it, but we do not store answers.";
        pl["GratisDna.Landing.EmployerNote"] = en["GratisDna.Landing.EmployerNote"];
        ro["GratisDna.Landing.EmployerNote"] = en["GratisDna.Landing.EmployerNote"];
        ar["GratisDna.Landing.EmployerNote"] = en["GratisDna.Landing.EmployerNote"];

        nl["GratisDna.Under16.Title"] = "Leuk dat je mee wilt doen!";
        en["GratisDna.Under16.Title"] = "Great that you want to join!";
        pl["GratisDna.Under16.Title"] = en["GratisDna.Under16.Title"];
        ro["GratisDna.Under16.Title"] = en["GratisDna.Under16.Title"];
        ar["GratisDna.Under16.Title"] = en["GratisDna.Under16.Title"];

        nl["GratisDna.Under16.Lead"] = "Onder de 16 heb je toestemming van je ouder of voogd nodig. Maak een account, dan vragen we die toestemming. Daarna kun je de test doen.";
        en["GratisDna.Under16.Lead"] = "Under 16 you need permission from your parent or guardian. Create an account and we will ask for that consent. Then you can take the test.";
        pl["GratisDna.Under16.Lead"] = en["GratisDna.Under16.Lead"];
        ro["GratisDna.Under16.Lead"] = en["GratisDna.Under16.Lead"];
        ar["GratisDna.Under16.Lead"] = en["GratisDna.Under16.Lead"];

        nl["GratisDna.Under16.Cta"] = "Account maken";
        en["GratisDna.Under16.Cta"] = "Create account";
        pl["GratisDna.Under16.Cta"] = "Utwórz konto";
        ro["GratisDna.Under16.Cta"] = "Creează cont";
        ar["GratisDna.Under16.Cta"] = "إنشاء حساب";

        nl["GratisDna.Questions.Progress"] = "Vraag {0} van {1}";
        en["GratisDna.Questions.Progress"] = "Question {0} of {1}";
        pl["GratisDna.Questions.Progress"] = en["GratisDna.Questions.Progress"];
        ro["GratisDna.Questions.Progress"] = en["GratisDna.Questions.Progress"];
        ar["GratisDna.Questions.Progress"] = en["GratisDna.Questions.Progress"];

        nl["GratisDna.Questions.ProgressCheer"] = "Vraag {0} van {1} · {2}";
        en["GratisDna.Questions.ProgressCheer"] = "Question {0} of {1} · {2}";
        pl["GratisDna.Questions.ProgressCheer"] = en["GratisDna.Questions.ProgressCheer"];
        ro["GratisDna.Questions.ProgressCheer"] = en["GratisDna.Questions.ProgressCheer"];
        ar["GratisDna.Questions.ProgressCheer"] = en["GratisDna.Questions.ProgressCheer"];

        nl["GratisDna.Questions.Cheer.0"] = "goed bezig! 💪";
        en["GratisDna.Questions.Cheer.0"] = "doing great! 💪";
        pl["GratisDna.Questions.Cheer.0"] = en["GratisDna.Questions.Cheer.0"];
        ro["GratisDna.Questions.Cheer.0"] = en["GratisDna.Questions.Cheer.0"];
        ar["GratisDna.Questions.Cheer.0"] = en["GratisDna.Questions.Cheer.0"];

        nl["GratisDna.Questions.Cheer.1"] = "zo door!";
        en["GratisDna.Questions.Cheer.1"] = "keep going!";
        pl["GratisDna.Questions.Cheer.1"] = en["GratisDna.Questions.Cheer.1"];
        ro["GratisDna.Questions.Cheer.1"] = en["GratisDna.Questions.Cheer.1"];
        ar["GratisDna.Questions.Cheer.1"] = en["GratisDna.Questions.Cheer.1"];

        nl["GratisDna.Questions.Cheer.2"] = "je zit erin!";
        en["GratisDna.Questions.Cheer.2"] = "you’re in the flow!";
        pl["GratisDna.Questions.Cheer.2"] = en["GratisDna.Questions.Cheer.2"];
        ro["GratisDna.Questions.Cheer.2"] = en["GratisDna.Questions.Cheer.2"];
        ar["GratisDna.Questions.Cheer.2"] = en["GratisDna.Questions.Cheer.2"];

        nl["GratisDna.Questions.Cheer.3"] = "bijna halfway";
        en["GratisDna.Questions.Cheer.3"] = "almost halfway";
        pl["GratisDna.Questions.Cheer.3"] = en["GratisDna.Questions.Cheer.3"];
        ro["GratisDna.Questions.Cheer.3"] = en["GratisDna.Questions.Cheer.3"];
        ar["GratisDna.Questions.Cheer.3"] = en["GratisDna.Questions.Cheer.3"];

        nl["GratisDna.Questions.Cheer.4"] = "laatste stukken!";
        en["GratisDna.Questions.Cheer.4"] = "final stretch!";
        pl["GratisDna.Questions.Cheer.4"] = en["GratisDna.Questions.Cheer.4"];
        ro["GratisDna.Questions.Cheer.4"] = en["GratisDna.Questions.Cheer.4"];
        ar["GratisDna.Questions.Cheer.4"] = en["GratisDna.Questions.Cheer.4"];

        nl["GratisDna.Questions.OfBlock"] = "Vraag {0} van {1}";
        en["GratisDna.Questions.OfBlock"] = "Question {0} of {1}";
        pl["GratisDna.Questions.OfBlock"] = en["GratisDna.Questions.OfBlock"];
        ro["GratisDna.Questions.OfBlock"] = en["GratisDna.Questions.OfBlock"];
        ar["GratisDna.Questions.OfBlock"] = en["GratisDna.Questions.OfBlock"];

        nl["GratisDna.Questions.Previous"] = "Vorige";
        en["GratisDna.Questions.Previous"] = "Previous";
        pl["GratisDna.Questions.Previous"] = en["GratisDna.Questions.Previous"];
        ro["GratisDna.Questions.Previous"] = en["GratisDna.Questions.Previous"];
        ar["GratisDna.Questions.Previous"] = en["GratisDna.Questions.Previous"];

        nl["GratisDna.Questions.Next"] = "Volgende";
        en["GratisDna.Questions.Next"] = "Next";
        pl["GratisDna.Questions.Next"] = en["GratisDna.Questions.Next"];
        ro["GratisDna.Questions.Next"] = en["GratisDna.Questions.Next"];
        ar["GratisDna.Questions.Next"] = en["GratisDna.Questions.Next"];

        nl["GratisDna.Questions.Later"] = "Later verder";
        en["GratisDna.Questions.Later"] = "Continue later";
        pl["GratisDna.Questions.Later"] = en["GratisDna.Questions.Later"];
        ro["GratisDna.Questions.Later"] = en["GratisDna.Questions.Later"];
        ar["GratisDna.Questions.Later"] = en["GratisDna.Questions.Later"];

        nl["GratisDna.Questions.Hint"] = "Twijfel je? Kies wat je het eerst voelt. Je antennes weten het vaak al.";
        en["GratisDna.Questions.Hint"] = "Not sure? Choose what you feel first. Your antennae often already know.";
        pl["GratisDna.Questions.Hint"] = en["GratisDna.Questions.Hint"];
        ro["GratisDna.Questions.Hint"] = en["GratisDna.Questions.Hint"];
        ar["GratisDna.Questions.Hint"] = en["GratisDna.Questions.Hint"];

        nl["GratisDna.Questions.Privacy"] = "Je antwoorden blijven op dit apparaat. Werkgevers zien ze nooit.";
        en["GratisDna.Questions.Privacy"] = "Your answers stay on this device. Employers never see them.";
        pl["GratisDna.Questions.Privacy"] = en["GratisDna.Questions.Privacy"];
        ro["GratisDna.Questions.Privacy"] = en["GratisDna.Questions.Privacy"];
        ar["GratisDna.Questions.Privacy"] = en["GratisDna.Questions.Privacy"];

        nl["GratisDna.Questions.SavedOnDevice"] = "Opgeslagen op dit apparaat";
        en["GratisDna.Questions.SavedOnDevice"] = "Saved on this device";
        pl["GratisDna.Questions.SavedOnDevice"] = en["GratisDna.Questions.SavedOnDevice"];
        ro["GratisDna.Questions.SavedOnDevice"] = en["GratisDna.Questions.SavedOnDevice"];
        ar["GratisDna.Questions.SavedOnDevice"] = en["GratisDna.Questions.SavedOnDevice"];

        nl["GratisDna.Likert.Aria"] = "{0}";
        en["GratisDna.Likert.Aria"] = "{0}";
        pl["GratisDna.Likert.Aria"] = en["GratisDna.Likert.Aria"];
        ro["GratisDna.Likert.Aria"] = en["GratisDna.Likert.Aria"];
        ar["GratisDna.Likert.Aria"] = en["GratisDna.Likert.Aria"];

        nl["GratisDna.Likert.AriaLow"] = "{0} – Past niet";
        en["GratisDna.Likert.AriaLow"] = "{0} – Does not fit";
        pl["GratisDna.Likert.AriaLow"] = en["GratisDna.Likert.AriaLow"];
        ro["GratisDna.Likert.AriaLow"] = en["GratisDna.Likert.AriaLow"];
        ar["GratisDna.Likert.AriaLow"] = en["GratisDna.Likert.AriaLow"];

        nl["GratisDna.Likert.AriaHigh"] = "{0} – Past wel";
        en["GratisDna.Likert.AriaHigh"] = "{0} – Fits well";
        pl["GratisDna.Likert.AriaHigh"] = en["GratisDna.Likert.AriaHigh"];
        ro["GratisDna.Likert.AriaHigh"] = en["GratisDna.Likert.AriaHigh"];
        ar["GratisDna.Likert.AriaHigh"] = en["GratisDna.Likert.AriaHigh"];

        nl["GratisDna.Result.Badge"] = "Eerste indruk · op basis van 20 vragen";
        en["GratisDna.Result.Badge"] = "First impression · based on 20 questions";
        pl["GratisDna.Result.Badge"] = en["GratisDna.Result.Badge"];
        ro["GratisDna.Result.Badge"] = en["GratisDna.Result.Badge"];
        ar["GratisDna.Result.Badge"] = en["GratisDna.Result.Badge"];

        nl["GratisDna.Result.Title"] = "Dit ben jij";
        en["GratisDna.Result.Title"] = "This is you";
        pl["GratisDna.Result.Title"] = en["GratisDna.Result.Title"];
        ro["GratisDna.Result.Title"] = en["GratisDna.Result.Title"];
        ar["GratisDna.Result.Title"] = en["GratisDna.Result.Title"];

        nl["GratisDna.Result.Lead"] = "Wauw, wat een mooi begin! Dit is een eerste blik op je werk-DNA. Hoe meer je invult, hoe scherper het beeld.";
        en["GratisDna.Result.Lead"] = "Wow, what a lovely start! This is a first look at your work DNA. The more you fill in, the sharper the picture.";
        pl["GratisDna.Result.Lead"] = en["GratisDna.Result.Lead"];
        ro["GratisDna.Result.Lead"] = en["GratisDna.Result.Lead"];
        ar["GratisDna.Result.Lead"] = en["GratisDna.Result.Lead"];

        nl["GratisDna.Result.Disclaimer"] = "Dit is een eerste indruk. Hoe meer vragen je beantwoordt, hoe preciezer het wordt.";
        en["GratisDna.Result.Disclaimer"] = "This is a first impression. The more questions you answer, the more precise it becomes.";
        pl["GratisDna.Result.Disclaimer"] = en["GratisDna.Result.Disclaimer"];
        ro["GratisDna.Result.Disclaimer"] = en["GratisDna.Result.Disclaimer"];
        ar["GratisDna.Result.Disclaimer"] = en["GratisDna.Result.Disclaimer"];

        nl["GratisDna.Passport.TabDna"] = "Mijn DNA";
        en["GratisDna.Passport.TabDna"] = "My DNA";
        pl["GratisDna.Passport.TabDna"] = en["GratisDna.Passport.TabDna"];
        ro["GratisDna.Passport.TabDna"] = en["GratisDna.Passport.TabDna"];
        ar["GratisDna.Passport.TabDna"] = en["GratisDna.Passport.TabDna"];

        nl["GratisDna.Passport.TabTests"] = "Mijn tests";
        en["GratisDna.Passport.TabTests"] = "My tests";
        pl["GratisDna.Passport.TabTests"] = en["GratisDna.Passport.TabTests"];
        ro["GratisDna.Passport.TabTests"] = en["GratisDna.Passport.TabTests"];
        ar["GratisDna.Passport.TabTests"] = en["GratisDna.Passport.TabTests"];

        nl["GratisDna.Passport.TabFit"] = "Past deze baan?";
        en["GratisDna.Passport.TabFit"] = "Does this job fit?";
        pl["GratisDna.Passport.TabFit"] = en["GratisDna.Passport.TabFit"];
        ro["GratisDna.Passport.TabFit"] = en["GratisDna.Passport.TabFit"];
        ar["GratisDna.Passport.TabFit"] = en["GratisDna.Passport.TabFit"];

        nl["GratisDna.Passport.TabCareer"] = "Carrière";
        en["GratisDna.Passport.TabCareer"] = "Career";
        pl["GratisDna.Passport.TabCareer"] = en["GratisDna.Passport.TabCareer"];
        ro["GratisDna.Passport.TabCareer"] = en["GratisDna.Passport.TabCareer"];
        ar["GratisDna.Passport.TabCareer"] = en["GratisDna.Passport.TabCareer"];

        nl["GratisDna.Passport.TabProof"] = "Bewijzen";
        en["GratisDna.Passport.TabProof"] = "Proof";
        pl["GratisDna.Passport.TabProof"] = en["GratisDna.Passport.TabProof"];
        ro["GratisDna.Passport.TabProof"] = en["GratisDna.Passport.TabProof"];
        ar["GratisDna.Passport.TabProof"] = en["GratisDna.Passport.TabProof"];

        nl["GratisDna.Passport.Locked"] = "Je volledige paspoort · na gratis account";
        en["GratisDna.Passport.Locked"] = "Your full passport · after a free account";
        pl["GratisDna.Passport.Locked"] = en["GratisDna.Passport.Locked"];
        ro["GratisDna.Passport.Locked"] = en["GratisDna.Passport.Locked"];
        ar["GratisDna.Passport.Locked"] = en["GratisDna.Passport.Locked"];

        nl["GratisDna.Signup.Title"] = "Bewaar je DNA en zie je hele paspoort";
        en["GratisDna.Signup.Title"] = "Save your DNA and see your full passport";
        pl["GratisDna.Signup.Title"] = en["GratisDna.Signup.Title"];
        ro["GratisDna.Signup.Title"] = en["GratisDna.Signup.Title"];
        ar["GratisDna.Signup.Title"] = en["GratisDna.Signup.Title"];

        nl["GratisDna.Signup.Sub"] = "Gratis. Je tests in je account zijn dan al klaar.";
        en["GratisDna.Signup.Sub"] = "Free. Your tests in your account will already be ready.";
        pl["GratisDna.Signup.Sub"] = en["GratisDna.Signup.Sub"];
        ro["GratisDna.Signup.Sub"] = en["GratisDna.Signup.Sub"];
        ar["GratisDna.Signup.Sub"] = en["GratisDna.Signup.Sub"];

        nl["GratisDna.Signup.Unlock.Passport"] = "Je volledige paspoort met Mijn DNA";
        en["GratisDna.Signup.Unlock.Passport"] = "Your full passport with My DNA";
        pl["GratisDna.Signup.Unlock.Passport"] = en["GratisDna.Signup.Unlock.Passport"];
        ro["GratisDna.Signup.Unlock.Passport"] = en["GratisDna.Signup.Unlock.Passport"];
        ar["GratisDna.Signup.Unlock.Passport"] = en["GratisDna.Signup.Unlock.Passport"];

        nl["GratisDna.Signup.Unlock.Fit"] = "“Past deze baan?” bij elke vacature";
        en["GratisDna.Signup.Unlock.Fit"] = "“Does this job fit?” on every vacancy";
        pl["GratisDna.Signup.Unlock.Fit"] = en["GratisDna.Signup.Unlock.Fit"];
        ro["GratisDna.Signup.Unlock.Fit"] = en["GratisDna.Signup.Unlock.Fit"];
        ar["GratisDna.Signup.Unlock.Fit"] = en["GratisDna.Signup.Unlock.Fit"];

        nl["GratisDna.Signup.Unlock.Map"] = "Banen op je reistijd-kaart";
        en["GratisDna.Signup.Unlock.Map"] = "Jobs on your travel-time map";
        pl["GratisDna.Signup.Unlock.Map"] = en["GratisDna.Signup.Unlock.Map"];
        ro["GratisDna.Signup.Unlock.Map"] = en["GratisDna.Signup.Unlock.Map"];
        ar["GratisDna.Signup.Unlock.Map"] = en["GratisDna.Signup.Unlock.Map"];

        nl["GratisDna.Signup.Unlock.Match"] = "Jouw top-match in Match";
        en["GratisDna.Signup.Unlock.Match"] = "Your top match in Match";
        pl["GratisDna.Signup.Unlock.Match"] = en["GratisDna.Signup.Unlock.Match"];
        ro["GratisDna.Signup.Unlock.Match"] = en["GratisDna.Signup.Unlock.Match"];
        ar["GratisDna.Signup.Unlock.Match"] = en["GratisDna.Signup.Unlock.Match"];

        nl["GratisDna.Signup.Carry"] = "Je 20 antwoorden gaan mee";
        en["GratisDna.Signup.Carry"] = "Your 20 answers come with you";
        pl["GratisDna.Signup.Carry"] = en["GratisDna.Signup.Carry"];
        ro["GratisDna.Signup.Carry"] = en["GratisDna.Signup.Carry"];
        ar["GratisDna.Signup.Carry"] = en["GratisDna.Signup.Carry"];

        nl["GratisDna.Tile.Title.Strength"] = "Zo werk jij";
        en["GratisDna.Tile.Title.Strength"] = "How you work";
        pl["GratisDna.Tile.Title.Strength"] = en["GratisDna.Tile.Title.Strength"];
        ro["GratisDna.Tile.Title.Strength"] = en["GratisDna.Tile.Title.Strength"];
        ar["GratisDna.Tile.Title.Strength"] = en["GratisDna.Tile.Title.Strength"];

        nl["GratisDna.Tile.Title.Riasec"] = "Dit vind je leuk";
        en["GratisDna.Tile.Title.Riasec"] = "What you enjoy";
        pl["GratisDna.Tile.Title.Riasec"] = en["GratisDna.Tile.Title.Riasec"];
        ro["GratisDna.Tile.Title.Riasec"] = en["GratisDna.Tile.Title.Riasec"];
        ar["GratisDna.Tile.Title.Riasec"] = en["GratisDna.Tile.Title.Riasec"];

        nl["GratisDna.Tile.Title.Culture"] = "Hier voel je je thuis";
        en["GratisDna.Tile.Title.Culture"] = "Where you feel at home";
        pl["GratisDna.Tile.Title.Culture"] = en["GratisDna.Tile.Title.Culture"];
        ro["GratisDna.Tile.Title.Culture"] = en["GratisDna.Tile.Title.Culture"];
        ar["GratisDna.Tile.Title.Culture"] = en["GratisDna.Tile.Title.Culture"];

        nl["GratisDna.Tile.Title.Value"] = "Dit vind je belangrijk";
        en["GratisDna.Tile.Title.Value"] = "What matters to you";
        pl["GratisDna.Tile.Title.Value"] = en["GratisDna.Tile.Title.Value"];
        ro["GratisDna.Tile.Title.Value"] = en["GratisDna.Tile.Title.Value"];
        ar["GratisDna.Tile.Title.Value"] = en["GratisDna.Tile.Title.Value"];

        nl["GratisDna.JobsTeaser.Title"] = "Meld je aan om te zien welke banen bij je passen";
        en["GratisDna.JobsTeaser.Title"] = "Sign up to see which jobs fit you";
        pl["GratisDna.JobsTeaser.Title"] = en["GratisDna.JobsTeaser.Title"];
        ro["GratisDna.JobsTeaser.Title"] = en["GratisDna.JobsTeaser.Title"];
        ar["GratisDna.JobsTeaser.Title"] = en["GratisDna.JobsTeaser.Title"];

        nl["GratisDna.JobsTeaser.Lead"] = "Vacatures in jouw buurt, gesorteerd op jouw DNA.";
        en["GratisDna.JobsTeaser.Lead"] = "Jobs near you, sorted by your DNA.";
        pl["GratisDna.JobsTeaser.Lead"] = en["GratisDna.JobsTeaser.Lead"];
        ro["GratisDna.JobsTeaser.Lead"] = en["GratisDna.JobsTeaser.Lead"];
        ar["GratisDna.JobsTeaser.Lead"] = en["GratisDna.JobsTeaser.Lead"];

        nl["GratisDna.Share.Title"] = "Deel je werk-DNA";
        en["GratisDna.Share.Title"] = "Share your work DNA";
        pl["GratisDna.Share.Title"] = en["GratisDna.Share.Title"];
        ro["GratisDna.Share.Title"] = en["GratisDna.Share.Title"];
        ar["GratisDna.Share.Title"] = en["GratisDna.Share.Title"];

        nl["GratisDna.Share.WhatsApp"] = "WhatsApp";
        en["GratisDna.Share.WhatsApp"] = "WhatsApp";
        pl["GratisDna.Share.WhatsApp"] = "WhatsApp";
        ro["GratisDna.Share.WhatsApp"] = "WhatsApp";
        ar["GratisDna.Share.WhatsApp"] = "WhatsApp";

        nl["GratisDna.Share.Instagram"] = "Instagram";
        en["GratisDna.Share.Instagram"] = "Instagram";
        pl["GratisDna.Share.Instagram"] = "Instagram";
        ro["GratisDna.Share.Instagram"] = "Instagram";
        ar["GratisDna.Share.Instagram"] = "Instagram";

        nl["GratisDna.Share.Generic"] = "Delen";
        en["GratisDna.Share.Generic"] = "Share";
        pl["GratisDna.Share.Generic"] = en["GratisDna.Share.Generic"];
        ro["GratisDna.Share.Generic"] = en["GratisDna.Share.Generic"];
        ar["GratisDna.Share.Generic"] = en["GratisDna.Share.Generic"];

        nl["GratisDna.Share.InstagramHint"] = "Afbeelding bewaard. Open Instagram en kies hem voor je story.";
        en["GratisDna.Share.InstagramHint"] = "Image saved. Open Instagram and add it to your story.";
        pl["GratisDna.Share.InstagramHint"] = en["GratisDna.Share.InstagramHint"];
        ro["GratisDna.Share.InstagramHint"] = en["GratisDna.Share.InstagramHint"];
        ar["GratisDna.Share.InstagramHint"] = en["GratisDna.Share.InstagramHint"];

        nl["GratisDna.Share.Copied"] = "Link gekopieerd";
        en["GratisDna.Share.Copied"] = "Link copied";
        pl["GratisDna.Share.Copied"] = en["GratisDna.Share.Copied"];
        ro["GratisDna.Share.Copied"] = en["GratisDna.Share.Copied"];
        ar["GratisDna.Share.Copied"] = en["GratisDna.Share.Copied"];

        nl["GratisDna.Share.CardTitle"] = "Mijn werk-DNA";
        en["GratisDna.Share.CardTitle"] = "My work DNA";
        pl["GratisDna.Share.CardTitle"] = en["GratisDna.Share.CardTitle"];
        ro["GratisDna.Share.CardTitle"] = en["GratisDna.Share.CardTitle"];
        ar["GratisDna.Share.CardTitle"] = en["GratisDna.Share.CardTitle"];

        nl["GratisDna.Share.CardFooter"] = "Ontdek jouw werk-DNA · lobsy.nl/dna";
        en["GratisDna.Share.CardFooter"] = "Discover your work DNA · lobsy.nl/dna";
        pl["GratisDna.Share.CardFooter"] = en["GratisDna.Share.CardFooter"];
        ro["GratisDna.Share.CardFooter"] = en["GratisDna.Share.CardFooter"];
        ar["GratisDna.Share.CardFooter"] = en["GratisDna.Share.CardFooter"];

        nl["GratisDna.Share.Text"] = "Ik deed de gratis werk-DNA-test op Lobsy.";
        en["GratisDna.Share.Text"] = "I took the free work DNA test on Lobsy.";
        pl["GratisDna.Share.Text"] = en["GratisDna.Share.Text"];
        ro["GratisDna.Share.Text"] = en["GratisDna.Share.Text"];
        ar["GratisDna.Share.Text"] = en["GratisDna.Share.Text"];

        nl["GratisDna.Preciezer.Title"] = "Wil je het preciezer?";
        en["GratisDna.Preciezer.Title"] = "Want more precision?";
        pl["GratisDna.Preciezer.Title"] = en["GratisDna.Preciezer.Title"];
        ro["GratisDna.Preciezer.Title"] = en["GratisDna.Preciezer.Title"];
        ar["GratisDna.Preciezer.Title"] = en["GratisDna.Preciezer.Title"];

        nl["GratisDna.Preciezer.FinishTests"] = "Maak de 4 tests af";
        en["GratisDna.Preciezer.FinishTests"] = "Finish the 4 tests";
        pl["GratisDna.Preciezer.FinishTests"] = en["GratisDna.Preciezer.FinishTests"];
        ro["GratisDna.Preciezer.FinishTests"] = en["GratisDna.Preciezer.FinishTests"];
        ar["GratisDna.Preciezer.FinishTests"] = en["GratisDna.Preciezer.FinishTests"];

        nl["GratisDna.Preciezer.FinishTestsMeta"] = "± 25 vragen per test · je 20 antwoorden tellen mee";
        en["GratisDna.Preciezer.FinishTestsMeta"] = "± 25 questions per test · your 20 answers count";
        pl["GratisDna.Preciezer.FinishTestsMeta"] = en["GratisDna.Preciezer.FinishTestsMeta"];
        ro["GratisDna.Preciezer.FinishTestsMeta"] = en["GratisDna.Preciezer.FinishTestsMeta"];
        ar["GratisDna.Preciezer.FinishTestsMeta"] = en["GratisDna.Preciezer.FinishTestsMeta"];

        nl["GratisDna.Preciezer.Deep"] = "Uitgebreide test met rapport";
        en["GratisDna.Preciezer.Deep"] = "Extended test with report";
        pl["GratisDna.Preciezer.Deep"] = en["GratisDna.Preciezer.Deep"];
        ro["GratisDna.Preciezer.Deep"] = en["GratisDna.Preciezer.Deep"];
        ar["GratisDna.Preciezer.Deep"] = en["GratisDna.Preciezer.Deep"];

        nl["GratisDna.Preciezer.DeepMeta"] = "150–200 vragen per test";
        en["GratisDna.Preciezer.DeepMeta"] = "150–200 questions per test";
        pl["GratisDna.Preciezer.DeepMeta"] = en["GratisDna.Preciezer.DeepMeta"];
        ro["GratisDna.Preciezer.DeepMeta"] = en["GratisDna.Preciezer.DeepMeta"];
        ar["GratisDna.Preciezer.DeepMeta"] = en["GratisDna.Preciezer.DeepMeta"];

        nl["GratisDna.Preciezer.Jobs"] = "Banen die bij je passen";
        en["GratisDna.Preciezer.Jobs"] = "Jobs that fit you";
        pl["GratisDna.Preciezer.Jobs"] = en["GratisDna.Preciezer.Jobs"];
        ro["GratisDna.Preciezer.Jobs"] = en["GratisDna.Preciezer.Jobs"];
        ar["GratisDna.Preciezer.Jobs"] = en["GratisDna.Preciezer.Jobs"];

        nl["GratisDna.Tag.Free"] = "Gratis";
        en["GratisDna.Tag.Free"] = "Free";
        pl["GratisDna.Tag.Free"] = en["GratisDna.Tag.Free"];
        ro["GratisDna.Tag.Free"] = en["GratisDna.Tag.Free"];
        ar["GratisDna.Tag.Free"] = en["GratisDna.Tag.Free"];

        nl["GratisDna.Tag.Paid"] = "Betaald";
        en["GratisDna.Tag.Paid"] = "Paid";
        pl["GratisDna.Tag.Paid"] = en["GratisDna.Tag.Paid"];
        ro["GratisDna.Tag.Paid"] = en["GratisDna.Tag.Paid"];
        ar["GratisDna.Tag.Paid"] = en["GratisDna.Tag.Paid"];

        nl["GratisDna.SharperLink"] = "Scherper beeld? In je account staan de volgende tests klaar.";
        en["GratisDna.SharperLink"] = "Want a sharper picture? The next tests are ready in your account.";
        pl["GratisDna.SharperLink"] = en["GratisDna.SharperLink"];
        ro["GratisDna.SharperLink"] = en["GratisDna.SharperLink"];
        ar["GratisDna.SharperLink"] = en["GratisDna.SharperLink"];

        nl["GratisDna.Wipe.Link"] = "Wis mijn antwoorden";
        en["GratisDna.Wipe.Link"] = "Delete my answers";
        pl["GratisDna.Wipe.Link"] = en["GratisDna.Wipe.Link"];
        ro["GratisDna.Wipe.Link"] = en["GratisDna.Wipe.Link"];
        ar["GratisDna.Wipe.Link"] = en["GratisDna.Wipe.Link"];

        nl["GratisDna.Wipe.Confirm"] = "Weet je het zeker? Je antwoorden op dit apparaat worden verwijderd.";
        en["GratisDna.Wipe.Confirm"] = "Are you sure? Your answers on this device will be removed.";
        pl["GratisDna.Wipe.Confirm"] = en["GratisDna.Wipe.Confirm"];
        ro["GratisDna.Wipe.Confirm"] = en["GratisDna.Wipe.Confirm"];
        ar["GratisDna.Wipe.Confirm"] = en["GratisDna.Wipe.Confirm"];

        nl["GratisDna.Wipe.Yes"] = "Ja, wis antwoorden";
        en["GratisDna.Wipe.Yes"] = "Yes, delete answers";
        pl["GratisDna.Wipe.Yes"] = en["GratisDna.Wipe.Yes"];
        ro["GratisDna.Wipe.Yes"] = en["GratisDna.Wipe.Yes"];
        ar["GratisDna.Wipe.Yes"] = en["GratisDna.Wipe.Yes"];

        nl["GratisDna.Wipe.No"] = "Annuleren";
        en["GratisDna.Wipe.No"] = "Cancel";
        pl["GratisDna.Wipe.No"] = en["GratisDna.Wipe.No"];
        ro["GratisDna.Wipe.No"] = en["GratisDna.Wipe.No"];
        ar["GratisDna.Wipe.No"] = en["GratisDna.Wipe.No"];

        nl["GratisDna.Sticky.Cta"] = "Bewaar je DNA · Maak gratis account";
        en["GratisDna.Sticky.Cta"] = "Save your DNA · Create a free account";
        pl["GratisDna.Sticky.Cta"] = en["GratisDna.Sticky.Cta"];
        ro["GratisDna.Sticky.Cta"] = en["GratisDna.Sticky.Cta"];
        ar["GratisDna.Sticky.Cta"] = en["GratisDna.Sticky.Cta"];

        nl["GratisDna.Sticky.Dismiss"] = "Later";
        en["GratisDna.Sticky.Dismiss"] = "Later";
        pl["GratisDna.Sticky.Dismiss"] = en["GratisDna.Sticky.Dismiss"];
        ro["GratisDna.Sticky.Dismiss"] = en["GratisDna.Sticky.Dismiss"];
        ar["GratisDna.Sticky.Dismiss"] = en["GratisDna.Sticky.Dismiss"];

        nl["GratisDna.Consent.Renew"] = "Onze toestemmingstekst is bijgewerkt. Geef opnieuw toestemming om je resultaat te zien.";
        en["GratisDna.Consent.Renew"] = "Our consent text was updated. Please consent again to see your result.";
        pl["GratisDna.Consent.Renew"] = en["GratisDna.Consent.Renew"];
        ro["GratisDna.Consent.Renew"] = en["GratisDna.Consent.Renew"];
        ar["GratisDna.Consent.Renew"] = en["GratisDna.Consent.Renew"];

        nl["GratisDna.QuietLink.Lead"] = "Eerst je werk-DNA ontdekken?";
        en["GratisDna.QuietLink.Lead"] = "Want to discover your work DNA first?";
        pl["GratisDna.QuietLink.Lead"] = en["GratisDna.QuietLink.Lead"];
        ro["GratisDna.QuietLink.Lead"] = en["GratisDna.QuietLink.Lead"];
        ar["GratisDna.QuietLink.Lead"] = en["GratisDna.QuietLink.Lead"];

        nl["GratisDna.QuietLink.Cta"] = "Doe de gratis test";
        en["GratisDna.QuietLink.Cta"] = "Take the free test";
        pl["GratisDna.QuietLink.Cta"] = en["GratisDna.QuietLink.Cta"];
        ro["GratisDna.QuietLink.Cta"] = en["GratisDna.QuietLink.Cta"];
        ar["GratisDna.QuietLink.Cta"] = en["GratisDna.QuietLink.Cta"];

        nl["GratisDna.RegisterBox.Title"] = "Je {0} antwoorden worden meegenomen";
        en["GratisDna.RegisterBox.Title"] = "Your {0} answers will be carried over";
        pl["GratisDna.RegisterBox.Title"] = en["GratisDna.RegisterBox.Title"];
        ro["GratisDna.RegisterBox.Title"] = en["GratisDna.RegisterBox.Title"];
        ar["GratisDna.RegisterBox.Title"] = en["GratisDna.RegisterBox.Title"];

        nl["GratisDna.RegisterBox.Lead"] = "Gratis. De tests in je onboarding zijn dan al klaar.";
        en["GratisDna.RegisterBox.Lead"] = "Free. The tests in your onboarding will already be done.";
        pl["GratisDna.RegisterBox.Lead"] = en["GratisDna.RegisterBox.Lead"];
        ro["GratisDna.RegisterBox.Lead"] = en["GratisDna.RegisterBox.Lead"];
        ar["GratisDna.RegisterBox.Lead"] = en["GratisDna.RegisterBox.Lead"];
    }
}
