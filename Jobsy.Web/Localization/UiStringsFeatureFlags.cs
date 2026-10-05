namespace Jobsy.Web.Localization;

public static class UiStringsFeatureFlags
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
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

        Add("Admin.EmployersEnabled",
            "Werkgevers actief",
            "Employers active",
            "Pracodawcy aktywni",
            "Angajatori activi",
            "أصحاب العمل نشطون");
        Add("Admin.EmployersEnabledHelp",
            "Uit = alleen zelfontdekking. Banenkaart, vacatures, sollicitaties en werkgeversportalen zijn dan verborgen en geblokkeerd. Er wordt niets verwijderd.",
            "Off = self-discovery only. Job map, vacancies, applications and employer portals are hidden and blocked. Nothing is deleted.",
            "Wył. = tylko samopoznanie. Mapa ofert, wakaty, aplikacje i portale pracodawców są ukryte i zablokowane. Nic nie jest usuwane.",
            "Oprit = doar auto-descoperire. Harta joburilor, posturile, candidaturile și portalele angajatorilor sunt ascunse și blocate. Nimic nu este șters.",
            "إيقاف = اكتشاف ذاتي فقط. خريطة الوظائف والشواغر والطلبات وبوابات أصحاب العمل مخفية وموقوفه. لا يُحذف شيء.");
        Add("Admin.CandidatePassportEnabled",
            "Mijn Paspoort (nieuw profiel)",
            "My Passport (new profile)",
            "Mój Paszport (nowy profil)",
            "Pașaportul meu (profil nou)",
            "جواز سفري (ملف جديد)");
        Add("Admin.CandidatePassportEnabledHelp",
            "Aan (standaard) = kandidaten zien Ontdekkingsreis, Mijn Paspoort, Zoeken, Sollicitaties, Carrière. Bewaard is een tab onder Sollicitaties. Uit = klassieke navigatie.",
            "On (default) = candidates see Discovery, My Passport, Search, Applications, Career. Saved is a tab under Applications. Off = classic navigation.",
            "Wł. (domyślnie) = kandydaci widzą Odkrywanie, Mój Paszport, Szukaj, Aplikacje, Kariera. Zapisane to zakładka. Wył. = klasyczna nawigacja.",
            "Pornit (implicit) = candidații văd Descoperire, Pașaportul meu, Căutare, Candidaturi, Carieră. Salvate e tab. Oprit = navigație clasică.",
            "تشغيل (افتراضي) = يرى المرشحون الاكتشاف وجواز سفري والبحث والطلبات والمسار. المحفوظات تبويب. إيقاف = التنقل الكلاسيكي.");
        Add("WgSoon.Title",
            "Voor werkgevers: binnenkort",
            "For employers: coming soon",
            "Dla pracodawców: wkrótce",
            "Pentru angajatori: în curând",
            "لأصحاب العمل: قريباً");
        Add("WgSoon.Lead",
            "Lobsy is nu eerst voor kandidaten. De omgeving voor werkgevers komt terug in een volgende fase.",
            "Lobsy is for candidates first right now. The employer area returns in a later phase.",
            "Lobsy jest teraz najpierw dla kandydatów. Strefa pracodawców wróci w kolejnej fazie.",
            "Lobsy este acum mai întâi pentru candidați. Zona angajatorilor revine într-o fază următoare.",
            "لوبسي الآن أولاً للمرشحين. ستعود بيئة أصحاب العمل في مرحلة لاحقة.");
        Add("WgSoon.CtaHome",
            "Naar de voorpagina",
            "To the home page",
            "Na stronę główną",
            "Spre pagina principală",
            "إلى الصفحة الرئيسية");
        Add("WgSoon.CandidateTitle",
            "Banen en sollicitaties: binnenkort",
            "Jobs and applications: coming soon",
            "Oferty i aplikacje: wkrótce",
            "Joburi și candidaturi: în curând",
            "الوظائف والطلبات: قريباً");
        Add("WgSoon.CandidateLead",
            "Die onderdelen komen later. Je paspoort, tests en loopbaanplan blijven van jou. Niets gaat naar een werkgever.",
            "Those parts come later. Your passport, tests and career plan stay yours. Nothing goes to an employer.",
            "Te części pojawią się później. Twój paszport, testy i plan kariery zostają twoje. Nic nie trafia do pracodawcy.",
            "Părțile acelea vin mai târziu. Pașaportul, testele și planul de carieră rămân ale tale. Nimic nu ajunge la un angajator.",
            "هذه الأجزاء تأتي لاحقاً. جوازك واختباراتك وخطة مسارك تبقى لك. لا شيء يذهب إلى صاحب عمل.");
        Add("WgSoon.CtaCandidate",
            "Naar mijn start",
            "To my start",
            "Do mojego startu",
            "Spre startul meu",
            "إلى بدايتي");
        Add("Admin.EmployersOffPill",
            "Werkgevers staan uit",
            "Employers are off",
            "Pracodawcy wyłączeni",
            "Angajatorii sunt opriți",
            "أصحاب العمل متوقفون");
        Add("AdminSettings.PassportPartners.Enabled.Title",
            "Paspoortpartners",
            "Passport partners",
            "Partnerzy paszportu",
            "Parteneri pașaport",
            "شركاء جواز السفر");
        Add("AdminSettings.PassportPartners.Enabled.Desc",
            "Uit = partnerportaal, codes en toestemming blijven verborgen. Er wordt niets verwijderd.",
            "Off = the partner portal, codes and consent stay hidden. Nothing is deleted.",
            "Wył. = portal partnera, kody i zgoda pozostają ukryte. Nic nie jest usuwane.",
            "Oprit = portalul partenerilor, codurile și consimțământul rămân ascunse. Nimic nu este șters.",
            "إيقاف = تبقى بوابة الشركاء والرموز والموافقة مخفية. لا يُحذف شيء.");
        Add("AdminSettings.PassportPdfV2.Enabled.Title",
            "Paspoort PDF v2",
            "Passport PDF v2",
            "Paszport PDF v2",
            "Pașaport PDF v2",
            "جواز السفر PDF v2");
        Add("AdminSettings.PassportPdfV2.Enabled.Desc",
            "Uit = kandidaten zien de extra deelbare voorkeuren niet. De PDF v2 komt in een latere stap.",
            "Off = candidates do not see the extra shareable preferences. PDF v2 arrives in a later step.",
            "Wył. = kandydaci nie widzą dodatkowych preferencji. PDF v2 przyjdzie później.",
            "Oprit = candidații nu văd preferințele partajabile. PDF v2 vine mai târziu.",
            "إيقاف = لا يرى المرشحون التفضيلات القابلة للمشاركة. ملف PDF v2 يأتي لاحقاً.");
        Add("AdminSettings.CompactTestPdf.Enabled.Title",
            "Korte testrapporten",
            "Short test reports",
            "Krótkie raporty testów",
            "Rapoarte scurte de test",
            "تقارير اختبار قصيرة");
        Add("AdminSettings.FreeCandidateTests.Enabled.Title",
            "Tests gratis voor iedereen",
            "Tests free for everyone",
            "Testy za darmo dla wszystkich",
            "Teste gratuite pentru toată lumea",
            "الاختبارات مجانية للجميع");
        Add("AdminSettings.FreeCandidateTests.Enabled.Desc",
            "Aan = kandidaten doen de uitgebreide test zonder te betalen. Uit = ze betalen weer. Een testaccount blijft altijd gratis.",
            "On = candidates take the extended test without paying. Off = they pay again. A test account stays free.",
            "Wł. = kandydaci robią test rozszerzony bez płatności. Wył. = znów płacą. Konto testowe zostaje darmowe.",
            "Pornit = candidații fac testul extins fără plată. Oprit = plătesc din nou. Un cont de test rămâne gratuit.",
            "تشغيل = يجري المرشحون الاختبار الموسّع دون دفع. إيقاف = يدفعون مرة أخرى. حساب الاختبار يبقى مجانياً.");
        Add("AdminSettings.FreeCandidateTests.Enabled.ImpactOff",
            "Uit: kandidaten moeten weer betalen voor de uitgebreide test.",
            "Off: candidates have to pay for the extended test again.",
            "Wył.: kandydaci znów płacą za test rozszerzony.",
            "Oprit: candidații trebuie să plătească din nou testul extins.",
            "إيقاف: يجب على المرشحين الدفع مرة أخرى للاختبار الموسّع.");
        Add("AdminSettings.HonestAdvice.Enabled.Title",
            "Eerlijk advies",
            "Honest advice",
            "Szczera rada",
            "Sfat sincer",
            "نصيحة صادقة");
        Add("AdminSettings.HonestAdvice.Enabled.Desc",
            "Aan = bij een beroep staat een kort opgeslagen advies. Uit = dat blok blijft verborgen. Er wordt niets live verzonnen.",
            "On = a job shows a short stored piece of advice. Off = that block stays hidden. Nothing is invented live.",
            "Wł. = przy zawodzie widać krótką zapisaną radę. Wył. = ten blok zostaje ukryty. Nic nie jest wymyślane na żywo.",
            "Pornit = la o meserie apare un sfat scurt salvat. Oprit = blocul rămâne ascuns. Nu se inventează nimic pe loc.",
            "تشغيل = تظهر نصيحة قصيرة محفوظة عند المهنة. إيقاف = يبقى هذا الجزء مخفياً. لا يُختلق شيء مباشرة.");
        Add("AdminSettings.CompactTestPdf.Enabled.Desc",
            "Aan = het uitgebreide persoonlijke testrapport is ongeveer 4 pagina's met grafieken. Uit = de langere rapporten van nu.",
            "On = the extended personal test report is about 4 pages with charts. Off = today's longer reports.",
            "Wł. = rozszerzony raport osobisty ma około 4 stron z wykresami. Wył. = dzisiejsze dłuższe raporty.",
            "Pornit = raportul personal extins are cam 4 pagini cu grafice. Oprit = rapoartele mai lungi de acum.",
            "تشغيل = التقرير الشخصي الموسّع حوالي 4 صفحات مع رسوم. إيقاف = التقارير الأطول الحالية.");
        Add("AdminSettings.PhoneVerification.Enabled.Title",
            "Telefoon bevestigen",
            "Confirm phone",
            "Potwierdź telefon",
            "Confirmă telefonul",
            "تأكيد الهاتف");
        Add("AdminSettings.PhoneVerification.Enabled.Desc",
            "Blijft uit tot er een sms-provider is. Er wordt nog geen sms verstuurd buiten Development.",
            "Stays off until an SMS provider is chosen. No SMS is sent outside Development.",
            "Pozostaje wyłączone do wyboru dostawcy SMS. Poza Development nie ma SMS.",
            "Rămâne oprit până la un furnizor SMS. Nu se trimite SMS în afara Development.",
            "يبقى متوقفاً حتى اختيار مزود رسائل. لا تُرسل رسالة خارج بيئة التطوير.");
    }
}
