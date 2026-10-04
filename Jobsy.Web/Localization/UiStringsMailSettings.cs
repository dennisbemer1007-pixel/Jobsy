namespace Jobsy.Web.Localization;

internal static class UiStringsMailSettings
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

        Add("MailUnsub.Title",
            "Afmelden",
            "Unsubscribe",
            "Wypisz się",
            "Dezabonare",
            "إلغاء الاشتراك");
        Add("MailUnsub.Submit",
            "Afmelden",
            "Unsubscribe",
            "Wypisz się",
            "Dezabonează-te",
            "إلغاء الاشتراك");
        Add("MailUnsub.ConfirmNamed",
            "Wil je geen {0} meer ontvangen?",
            "Do you want to stop receiving {0}?",
            "Czy chcesz przestać otrzymywać {0}?",
            "Vrei să nu mai primești {0}?",
            "هل تريد التوقف عن استلام {0}؟");
        Add("MailUnsub.Done",
            "Je krijgt geen herinneringen meer per e-mail. Je kunt dit weer aanzetten in je instellingen.",
            "You will not get reminder emails anymore. You can turn this on again in your settings.",
            "Nie dostaniesz już przypomnień e-mailem. Możesz to włączyć znowu w ustawieniach.",
            "Nu mai primești e-mailuri de reamintire. Poți porni asta din nou în setări.",
            "لن تصلك تذكيرات بالبريد بعد الآن. يمكنك تشغيلها مرة أخرى من الإعدادات.");
        Add("MailUnsub.OptIn",
            "Toch weer aanzetten",
            "Turn back on",
            "Włącz ponownie",
            "Reactivează",
            "إعادة التفعيل");
        Add("MailUnsub.OptInDone",
            "Je ontvangt deze mails weer.",
            "You will receive these emails again.",
            "Znów będziesz otrzymywać te e-maile.",
            "Vei primi din nou aceste e-mailuri.",
            "ستصلك هذه الرسائل مجددًا.");
        Add("MailUnsub.Invalid",
            "Deze link is ongeldig of verlopen.",
            "This link is invalid or expired.",
            "Ten link jest nieprawidłowy lub wygasł.",
            "Această legătură este invalidă sau a expirat.",
            "هذا الرابط غير صالح أو منتهٍ.");
        Add("MailUnsub.SettingsLink",
            "Naar mail-instellingen",
            "Go to mail settings",
            "Przejdź do ustawień poczty",
            "Mergi la setările de e-mail",
            "إلى إعدادات البريد");
        Add("MailUnsub.Seo.Title", "Afmelden voor mails", "Unsubscribe from emails", "Wypisz się z e-maili", "Dezabonare e-mail", "إلغاء اشتراك البريد");
        Add("MailUnsub.Seo.Description", "Bevestig of je optionele Lobsy-mails wilt stopzetten.", "Confirm stopping optional Lobsy emails.", "Potwierdź wypisanie z opcjonalnych e-maili Lobsy.", "Confirmă oprirea e-mailurilor opționale Lobsy.", "أكد إيقاف رسائل Lobsy الاختيارية.");

        Add("MailSettings.Title", "Mail-instellingen", "Mail settings", "Ustawienia e-mail", "Setări e-mail", "إعدادات البريد");
        Add("MailSettings.Reminders.Label",
            "Herinneringen per e-mail",
            "Reminder emails",
            "Przypomnienia e-mailem",
            "E-mailuri de reamintire",
            "تذكيرات بالبريد");
        Add("MailSettings.Reminders.Hint",
            "Zet dit uit als je geen herinneringen per e-mail wilt. Een seintje op je telefoon en WhatsApp blijven apart",
            "Turn this off if you do not want reminder emails. A phone alert and WhatsApp stay separate",
            "Wyłącz to, jeśli nie chcesz przypomnień e-mailem. Alert w telefonie i WhatsApp zostają osobno",
            "Oprește asta dacă nu vrei e-mailuri de reamintire. O alertă pe telefon și WhatsApp rămân separat",
            "أوقف هذا إذا كنت لا تريد تذكيرات بالبريد. تنبيه الهاتف وواتساب يبقيان منفصلين");
        Add("MailSettings.OptionalHeading", "Optionele mails", "Optional emails", "Opcjonalne e-maile", "E-mailuri opționale", "رسائل اختيارية");
        Add("MailSettings.AlwaysHeading", "Deze mails krijg je altijd", "You always get these emails", "Te e-maile dostajesz zawsze", "Primești întotdeauna aceste e-mailuri", "تصلك هذه الرسائل دائمًا");
        Add("MailSettings.AlwaysHint",
            "Codes, sollicitaties, uitnodigingen en beveiliging kun je niet uitzetten.",
            "Codes, applications, invites and security mail cannot be turned off.",
            "Kodów, aplikacji, zaproszeń i bezpieczeństwa nie da się wyłączyć.",
            "Codurile, candidaturile, invitațiile și securitatea nu se pot opri.",
            "لا يمكن إيقاف الرموز والطلبات والدعوات وأمان الحساب.");
        Add("MailSettings.NoOptional",
            "Voor jouw rol zijn er geen optionele mails.",
            "Your role has no optional emails.",
            "Twoja rola nie ma opcjonalnych e-maili.",
            "Rolul tău nu are e-mailuri opționale.",
            "لا توجد رسائل اختيارية لدورك.");
        Add("MailSettings.Save", "Opslaan", "Save", "Zapisz", "Salvează", "حفظ");
        Add("MailSettings.Saved", "Opgeslagen.", "Saved.", "Zapisano.", "Salvat.", "تم الحفظ.");
        Add("MailSettings.LoadError",
            "Kon de instellingen niet laden. Probeer later opnieuw.",
            "Could not load settings. Try again later.",
            "Nie udało się wczytać ustawień. Spróbuj później.",
            "Nu am putut încărca setările. Încearcă mai târziu.",
            "تعذر تحميل الإعدادات. حاول لاحقًا.");
        Add("MailSettings.PushBom.Label",
            "Tips over vacatures bij jou in de buurt",
            "Tips about nearby vacancies",
            "Wskazówki o ofertach w okolicy",
            "Sfaturi despre joburi din apropiere",
            "نصائح حول وظائف قريبة منك");
        Add("MailSettings.PushBom.Hint",
            "Af en toe een tip over een passende vacature dichtbij.",
            "Occasional tips about a matching nearby vacancy.",
            "Od czasu do czasu wskazówka o pasującej ofercie w pobliżu.",
            "Din când în când un sfat despre un job potrivit aproape.",
            "من حين لآخر نصيحة حول وظيفة مناسبة قريبة.");
        Add("MailSettings.VacancyEngagement.Label",
            "Herinnering als je vacature 14 dagen openstaat",
            "Reminder when a vacancy has been open 14 days",
            "Przypomnienie, gdy oferta jest otwarta 14 dni",
            "Reminder când un job e deschis de 14 zile",
            "تذكير عندما تبقى وظيفة مفتوحة 14 يومًا");
        Add("MailSettings.VacancyEngagement.Hint",
            "Alleen als je vacature weinig reacties krijgt.",
            "Only when your vacancy gets few responses.",
            "Tylko gdy oferta ma mało odpowiedzi.",
            "Doar când jobul are puține răspunsuri.",
            "فقط عندما تحصل وظيفتك على ردود قليلة.");
        Add("MailSettings.CompanyReEngagement.Label",
            "Bericht als je een tijd niet actief was",
            "Message when you have been inactive for a while",
            "Wiadomość, gdy długo nie byłeś aktywny",
            "Mesaj când ai fost inactiv o perioadă",
            "رسالة عندما تكون غير نشط لفترة");
        Add("MailSettings.CompanyReEngagement.Hint",
            "Een seintje om weer in te loggen als je account stilstaat.",
            "A nudge to sign in again when your account is quiet.",
            "Przypomnienie o zalogowaniu, gdy konto milczy.",
            "Un impuls să te autentifici din nou când contul e quiet.",
            "تنبيه لتسجيل الدخول مجددًا عندما يكون حسابك هادئًا.");
        Add("MailSettings.Seo.Title", "Mail-instellingen", "Mail settings", "Ustawienia e-mail", "Setări e-mail", "إعدادات البريد");
        Add("MailSettings.Seo.Description", "Beheer optionele Lobsy-mails.", "Manage optional Lobsy emails.", "Zarządzaj opcjonalnymi e-mailami Lobsy.", "Gestionează e-mailurile opționale Lobsy.", "إدارة رسائل Lobsy الاختيارية.");
    }
}
