namespace Jobsy.Web.Localization;

/// <summary>Plain warm candidate public shell (landing + login), mockup kandidaat/plain.</summary>
public static class UiStringsPlainPublic
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string? plText = null, string? roText = null, string? arText = null)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText ?? enText;
            ro[key] = roText ?? enText;
            ar[key] = arText ?? enText;
        }

        Add("PlainPublic.Landing.SeoTitle", "Lobsy", "Lobsy");
        Add("PlainPublic.Landing.SeoDescription",
            "Werk dat bij je past. Rustig gevonden. Gratis account, ongeveer twintig minuten.",
            "Work that fits you. Found calmly. Free account, about twenty minutes.");

        Add("PlainPublic.Landing.Eyebrow", "Voor werkzoekenden", "For job seekers");
        Add("PlainPublic.Landing.Title", "Werk dat bij je past. Rustig gevonden.", "Work that fits you. Found calmly.");
        Add("PlainPublic.Landing.Lead",
            "Je vertelt eerst wie je bent en wat je kunt, daarna pas kijk je naar vacatures. Voor iedereen, met of zonder diploma, jong of wat ouder.",
            "You share who you are and what you can do before you browse vacancies. For everyone, with or without a diploma, young or a bit older.");
        Add("PlainPublic.Landing.CtaCreate", "Account maken", "Create account");
        Add("PlainPublic.Landing.CtaLogin", "Ik heb al een account", "I already have an account");
        Add("PlainPublic.Landing.Meta",
            "Gratis. Ongeveer 20 minuten. Je kunt altijd pauzeren en later verder.",
            "Free. About 20 minutes. You can pause anytime and continue later.");

        Add("PlainPublic.Landing.HowTitle", "Zo werkt het", "How it works");
        Add("PlainPublic.Landing.Step1Title", "Start", "Start");
        Add("PlainPublic.Landing.Step1Body", "Account en toestemming", "Account and consent");
        Add("PlainPublic.Landing.Step2Title", "Over jou", "About you");
        Add("PlainPublic.Landing.Step2Body", "Wanneer en waar je kunt werken", "When and where you can work");
        Add("PlainPublic.Landing.Step3Title", "Werk en leren", "Work and learning");
        Add("PlainPublic.Landing.Step3Body", "Ervaring, opleiding en droombaan", "Experience, education and dream job");
        Add("PlainPublic.Landing.Step4Title", "Tests", "Tests");
        Add("PlainPublic.Landing.Step4Body", "Vier korte tests, zonder goed of fout", "Four short tests, no right or wrong");
        Add("PlainPublic.Landing.Step5Title", "Resultaat", "Result");
        Add("PlainPublic.Landing.Step5Body", "Jouw Kompas en je paspoort", "Your Compass and your passport");

        Add("PlainPublic.Landing.Card1Title", "Eerst jij, dan het werk", "You first, then the job");
        Add("PlainPublic.Landing.Card1Body", "Werkgevers zien meer dan een cv.", "Employers see more than a CV.");
        Add("PlainPublic.Landing.Card2Title", "Geen goed of fout", "No right or wrong");
        Add("PlainPublic.Landing.Card2Body", "Je ziet woorden die bij je passen, geen cijfers.", "You see words that fit you, not numbers.");
        Add("PlainPublic.Landing.Card3Title", "Jij houdt de regie", "You stay in control");
        Add("PlainPublic.Landing.Card3Body", "Jij bepaalt wat werkgevers zien.", "You decide what employers see.");

        Add("PlainPublic.Passport.Eyebrow", "Resultaat · klaar om te delen", "Result · ready to share");
        Add("PlainPublic.Passport.Title", "Je paspoort", "Your passport");
        Add("PlainPublic.Passport.Lead",
            "Dit is wie je bent, in je eigen woorden. Jij bepaalt wat werkgevers zien.",
            "This is who you are, in your own words. You decide what employers see.");
        Add("PlainPublic.Passport.StepLineBold", "Stap {0} van {1}", "Step {0} of {1}");
        Add("PlainPublic.Passport.GuideTitle", "Je gids", "Your guide");
        Add("PlainPublic.Passport.GuideSubtitle", "Zo gebruiken we dit", "How we use this");
        Add("PlainPublic.Passport.GuideBody",
            "Je paspoort laat werkgevers zien wie je bent. Jij kiest wie het ziet. Je naam blijft verborgen tot een match.",
            "Your passport shows employers who you are. You choose who sees it. Your name stays hidden until a match.");
        Add("PlainPublic.Passport.VisibilityTitle", "Zichtbaarheid", "Visibility");
        Add("PlainPublic.Passport.VisibilityEmployers", "Zichtbaar voor werkgevers", "Visible to employers");
        Add("PlainPublic.Passport.VisibilityEmployersHint",
            "Werkgevers met een passende vacature kunnen je paspoort zien.",
            "Employers with a matching vacancy can see your passport.");
        Add("PlainPublic.Passport.VisibilityName", "Naam pas tonen na een match", "Show name after a match");
        Add("PlainPublic.Passport.VisibilityNameHint",
            "Tot die tijd zien werkgevers alleen je voornaam.",
            "Until then employers only see your first name.");
        Add("PlainPublic.Passport.KvWork", "Werkervaring", "Work experience");
        Add("PlainPublic.Passport.KvEducation", "Opleiding", "Education");
        Add("PlainPublic.Passport.KvDream", "Droom", "Dream role");
        Add("PlainPublic.Passport.KvAvailable", "Beschikbaar", "Availability");
        Add("PlainPublic.Passport.Updated", "Bijgewerkt {0}", "Updated {0}");
        Add("PlainPublic.Footer.Accessibility", "Toegankelijkheid", "Accessibility");
        Add("PlainPublic.Footer.Contact", "Contact", "Contact");

        Add("PlainPublic.Login.TabLogin", "Inloggen", "Log in");
        Add("PlainPublic.Login.TabSignup", "Account maken", "Create account");
        Add("PlainPublic.Login.Title", "Fijn dat je er bent", "Good to see you");
        Add("PlainPublic.Login.Lead",
            "Log in met je e-mailadres. Nog geen account? Dat maak je in een minuut.",
            "Log in with your email address. No account yet? You can create one in a minute.");
        Add("PlainPublic.Login.Help",
            "Lukt het niet? Je adviseur of de helpdesk helpt je graag.",
            "Having trouble? Your adviser or the helpdesk will gladly help.");

        Add("PlainPublic.Signup.Title", "Account maken", "Create account");
        Add("PlainPublic.Signup.Lead",
            "Vul je e-mailadres in. We sturen je een code om je account te bevestigen.",
            "Enter your email address. We will send a code to confirm your account.");

        Add("PlainPublic.Forgot.Title", "Wachtwoord vergeten", "Forgot password");
        Add("PlainPublic.Forgot.Lead",
            "Vul je e-mailadres in. Je ontvangt een link om een nieuw wachtwoord te kiezen.",
            "Enter your email address. You will receive a link to choose a new password.");
        Add("PlainPublic.Forgot.SentTitle", "Check je e-mail", "Check your email");
        Add("PlainPublic.Forgot.SentLead",
            "Als dit adres bij ons bekend is, sturen we instructies om je wachtwoord te wijzigen.",
            "If this address is known to us, we will send instructions to reset your password.");
    }
}
