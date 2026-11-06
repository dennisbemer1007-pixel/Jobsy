namespace Jobsy.Web.Localization;

public static class UiStringsTestReflection
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
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

        Add("TestReflection.ThemeLabel", "Thema", "Theme", "Theme", "Theme", "Theme");
        Add("TestReflection.PracticeLabel", "Sterk in de praktijk", "Strong in practice", "Strong in practice", "Strong in practice", "Strong in practice");
        Add("TestReflection.WatchLabel", "Waar je op let", "What to watch for", "What to watch for", "What to watch for", "What to watch for");
        Add("TestReflection.EnergyLabel", "Energie", "Energy", "Energy", "Energy", "Energy");
        Add("TestReflection.EnergyHeading",
            "Wat je oplaadt en wat je leegtrekt",
            "What charges you and what drains you",
            "What charges you and what drains you",
            "What charges you and what drains you",
            "What charges you and what drains you");
        Add("TestReflection.EnergyGives", "Geeft energie", "Gives energy", "Gives energy", "Gives energy", "Gives energy");
        Add("TestReflection.EnergyDrains", "Kost energie", "Drains energy", "Drains energy", "Drains energy", "Drains energy");
        Add("TestReflection.EnvironmentLabel", "Waar je tot je recht komt", "Where you thrive", "Where you thrive", "Where you thrive", "Where you thrive");
        Add("TestReflection.Recognize", "Herken je jezelf hierin?", "Do you recognize yourself?", "Do you recognize yourself?", "Do you recognize yourself?", "Do you recognize yourself?");
        Add("TestReflection.Feedback.Yes", "Ja, dit klopt", "Yes, this fits", "Yes, this fits", "Yes, this fits", "Yes, this fits");
        Add("TestReflection.Feedback.Partial", "Deels", "Partly", "Partly", "Partly", "Partly");
        Add("TestReflection.Feedback.No", "Niet echt", "Not really", "Not really", "Not really", "Not really");
        Add("TestReflection.Privacy",
            "Deze spiegel is alleen voor jou. Hij komt niet in je paspoort en werkgevers of partners zien hem niet.",
            "This mirror is only for you. It is not in your passport and employers or partners do not see it.",
            "This mirror is only for you. It is not in your passport and employers or partners do not see it.",
            "This mirror is only for you. It is not in your passport and employers or partners do not see it.",
            "This mirror is only for you. It is not in your passport and employers or partners do not see it.");
        Add("TestReflection.Bridge.Eyebrow", "Nog een laag dieper", "One layer deeper", "One layer deeper", "One layer deeper", "One layer deeper");
        Add("TestReflection.Bridge.Title", "Wil je weten wat hier nog onder zit?", "Want to know what is underneath?", "Want to know what is underneath?", "Want to know what is underneath?", "Want to know what is underneath?");
        Add("TestReflection.Bridge.Lead",
            "Je spiegel laat zien hoe je werkt. De uitgebreide test kijkt een laag dieper en veel preciezer.",
            "Your mirror shows how you work. The extended test goes one layer deeper and much more precisely.",
            "Your mirror shows how you work. The extended test goes one layer deeper and much more precisely.",
            "Your mirror shows how you work. The extended test goes one layer deeper and much more precisely.",
            "Your mirror shows how you work. The extended test goes one layer deeper and much more precisely.");
        Add("TestReflection.Bridge.Fine",
            "Je spiegel blijft gratis van jou.",
            "Your mirror stays free for you.",
            "Your mirror stays free for you.",
            "Your mirror stays free for you.",
            "Your mirror stays free for you.");
        Add("TestReflection.ScoresAccordion", "Je antwoorden per eigenschap", "Your answers per trait", "Your answers per trait", "Your answers per trait", "Your answers per trait");
        Add("TestReflection.ScoresAccordionHint", "de verdeling achter deze spiegel", "the breakdown behind this mirror", "the breakdown behind this mirror", "the breakdown behind this mirror", "the breakdown behind this mirror");
        Add("TestReflection.AboutAccordion", "Over deze test", "About this test", "About this test", "About this test", "About this test");
        Add("TestReflection.AboutAccordionHint", "model, bronnen en hoe we dit schrijven", "model, sources and how we write this", "model, sources and how we write this", "model, sources and how we write this", "model, sources and how we write this");
        Add("TestReflection.Bridge.Cta", "Bekijk wat je krijgt", "See what you get", "See what you get", "See what you get", "See what you get");
        Add("TestReflection.Bridge.Benefit.Facets",
            "Per eigenschap de kanten eronder, zodat je ziet wáár je kracht precies zit",
            "For each trait, the facets underneath so you see exactly where your strength sits",
            "For each trait, the facets underneath so you see exactly where your strength sits",
            "For each trait, the facets underneath so you see exactly where your strength sits",
            "For each trait, the facets underneath so you see exactly where your strength sits");
        Add("TestReflection.Bridge.Benefit.Comparison",
            "Hoe je je verhoudt tot anderen die de test deden",
            "How you compare to others who took the test",
            "How you compare to others who took the test",
            "How you compare to others who took the test",
            "How you compare to others who took the test");
        Add("TestReflection.Bridge.Benefit.ActionPlan",
            "Een actieplan met drie concrete stappen",
            "An action plan with three concrete steps",
            "An action plan with three concrete steps",
            "An action plan with three concrete steps",
            "An action plan with three concrete steps");
        Add("TestReflection.Bridge.Benefit.Pdf",
            "Een rapport als PDF voor je sollicitaties",
            "A report as a PDF for your applications",
            "A report as a PDF for your applications",
            "A report as a PDF for your applications",
            "A report as a PDF for your applications");
        Add("TestReflection.Bridge.Meta",
            "{0} vragen · ca. {1} min · eenmalig € {2} · {3}",
            "{0} questions · approx. {1} min · one-off € {2} · {3}",
            "{0} questions · approx. {1} min · one-off € {2} · {3}",
            "{0} questions · approx. {1} min · one-off € {2} · {3}",
            "{0} questions · approx. {1} min · one-off € {2} · {3}");
    }
}
