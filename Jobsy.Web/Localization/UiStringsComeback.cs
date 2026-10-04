namespace Jobsy.Web.Localization;

internal static class UiStringsComeback
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

        Add("MailSettings.Comeback.Label",
            "Herinnering om terug te komen",
            "A reminder to come back",
            "Przypomnienie, żeby wrócić",
            "Reamintire să revii",
            "تذكير للعودة");
        Add("MailSettings.Comeback.Hint",
            "Eén mail als je 4 korte tests nog openstaan, en een mail als je 4 weken niet kijkt. Hooguit 2 per maand. Standaard uit.",
            "One email if your 4 short tests are still open, and one if you stay away for 4 weeks. At most 2 a month. Off unless you turn it on.",
            "Jeden e-mail, gdy 4 krótkie testy są otwarte, i jeden po 4 tygodniach ciszy. Najwyżej 2 na miesiąc. Domyślnie wyłączone.",
            "Un e-mail dacă cele 4 teste scurte sunt deschise, și unul după 4 săptămâni. Cel mult 2 pe lună. Oprit până îl pornești.",
            "رسالة إذا كانت اختباراتك الأربعة مفتوحة، ورسالة بعد 4 أسابيع من الغياب. رسالتان كحد أقصى في الشهر. متوقف حتى تفعّله.");
        Add("Comeback.WhatsApp.Title",
            "Herinnering via WhatsApp",
            "Reminder by WhatsApp",
            "Przypomnienie przez WhatsApp",
            "Reamintire prin WhatsApp",
            "تذكير عبر واتساب");
        Add("Comeback.WhatsApp.Hint",
            "Alleen als jij dit aanzet. We sturen niets naar anderen.",
            "Only if you turn this on. We do not message anyone else.",
            "Tylko gdy sam to włączysz. Nie piszemy do nikogo innego.",
            "Doar dacă pornești tu asta. Nu scriem nimănui altcuiva.",
            "فقط إذا فعّلت هذا. لا نرسل لأحد غيرك.");
        Add("Comeback.WhatsApp.Phone",
            "Jouw telefoonnummer",
            "Your phone number",
            "Twój numer telefonu",
            "Numărul tău de telefon",
            "رقم هاتفك");
        Add("Comeback.WhatsApp.PhoneHint",
            "Bijvoorbeeld 06 12345678.",
            "For example 06 12345678.",
            "Na przykład 06 12345678.",
            "De exemplu 06 12345678.",
            "مثلاً 06 12345678.");
        Add("AdminSettings.WhatsAppReminders.Enabled.Title",
            "WhatsApp-herinneringen",
            "WhatsApp reminders",
            "Przypomnienia WhatsApp",
            "Reamintiri WhatsApp",
            "تذكيرات واتساب");
        Add("AdminSettings.WhatsAppReminders.Enabled.Desc",
            "Uit = er gaat geen WhatsApp. Kandidaten zien de keuze niet. Standaard uit.",
            "Off = no WhatsApp is sent. Candidates do not see the choice. Off by default.",
            "Wył. = nie ma WhatsApp. Kandydaci nie widzą wyboru. Domyślnie wyłączone.",
            "Oprit = nu pleacă WhatsApp. Candidații nu văd opțiunea. Oprit implicit.",
            "إيقاف = لا يُرسل واتساب. المرشحون لا يرون الخيار. متوقف افتراضياً.");
        Add("AdminSettings.WhatsAppReminders.Enabled.Impact",
            "Aan = alleen kandidaten die zelf ja zeggen en een nummer invullen kunnen een WhatsApp krijgen.",
            "On = only candidates who say yes and enter a number can get a WhatsApp.",
            "Wł. = WhatsApp dostają tylko kandydaci, którzy sami się zgodzą i podadzą numer.",
            "Pornit = doar candidații care spun da și scriu un număr pot primi WhatsApp.",
            "تشغيل = فقط من يوافق ويكتب رقمه يمكن أن يصله واتساب.");
    }
}
