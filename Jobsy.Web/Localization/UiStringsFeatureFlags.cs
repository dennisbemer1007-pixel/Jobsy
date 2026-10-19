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
        Add("WgSoon.EmployerCard1Title",
            "Vacatures voorbereiden",
            "Prepare vacancies",
            "Przygotuj oferty",
            "Pregătește posturi",
            "جهّز الشواغر");
        Add("WgSoon.EmployerCard1Lead",
            "Bekijk hoe vacatures straks worden aangemaakt en beheerd — zonder illustraties, wel overzichtelijk.",
            "See how vacancies will be created and managed — clear and professional.",
            "Zobacz, jak oferty będą tworzone i zarządzane — przejrzyście.",
            "Vezi cum vor fi create și gestionate posturile — clar.",
            "اطّلع على كيفية إنشاء الشواغر وإدارتها — بشكل واضح.");
        Add("WgSoon.EmployerCard1Cta",
            "Inloggen als werkgever",
            "Sign in as employer",
            "Zaloguj jako pracodawca",
            "Autentificare angajator",
            "تسجيل الدخول كصاحب عمل");
        Add("WgSoon.EmployerCard2Title",
            "Hoe Lobsy werkt",
            "How Lobsy works",
            "Jak działa Lobsy",
            "Cum funcționează Lobsy",
            "كيف يعمل Lobsy");
        Add("WgSoon.EmployerCard2Lead",
            "Korte uitleg over sollicitaties, privacy en wat kandidaten wel en niet zien.",
            "A short explanation of applications, privacy, and what candidates see.",
            "Krótko o aplikacjach, prywatności i tym, co widzą kandidaci.",
            "Pe scurt despre candidaturi, confidențialitate și ce văd candidații.",
            "شرح قصير للطلبات والخصوصية وما يراه المرشحون.");
        Add("WgSoon.EmployerCard2Cta",
            "Lees de werkgeversgids",
            "Read the employer guide",
            "Przeczytaj przewodnik",
            "Citește ghidul",
            "اقرأ دليل صاحب العمل");
        Add("WgSoon.EmployerCard3Title",
            "Contact",
            "Contact",
            "Kontakt",
            "Contact",
            "تواصل");
        Add("WgSoon.EmployerCard3Lead",
            "Vragen over timing of een pilot? We reageren zakelijk en zonder verkooppraat.",
            "Questions about timing or a pilot? We reply professionally.",
            "Pytania o termin lub pilotaż? Odpowiadamy rzeczowo.",
            "Întrebări despre calendar sau pilot? Răspundem profesional.",
            "أسئلة عن التوقيت أو تجربة؟ نرد بشكل مهني.");
        Add("WgSoon.EmployerCard3Cta",
            "Neem contact op",
            "Contact us",
            "Skontaktuj się",
            "Contactează-ne",
            "تواصل معنا");
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
        Add("AdminSettings.PassportPdfDnaBlocks.Enabled.Title",
            "Paspoort-PDF: DNA-blokken",
            "Passport PDF: DNA blocks",
            "PDF paszportu: bloki DNA",
            "PDF pașaport: blocuri ADN",
            "PDF جواز السفر: كتل DNA");
        Add("AdminSettings.PassportPdfDnaBlocks.Enabled.Desc",
            "Uit = geen “Mijn DNA in 4 lagen”, “Hoe ik graag werk” en “Zo haal je het beste uit …” op de PDF.",
            "Off = omit “My DNA in 4 layers”, “How I like to work” and “How to get the best out of …” on the PDF.",
            "Wył. = bez bloków DNA, “Jak lubię pracować” i “Jak wydobyć z … to, co najlepsze” w PDF.",
            "Oprit = fără “ADN-ul meu în 4 straturi”, “Cum îmi place să lucrez” și “Cum scoți ce e mai bun din …” în PDF.",
            "إيقاف = بدون “DNA في 4 طبقات” و“كيف أحب العمل” و“كيف تستخرج الأفضل من …” في PDF.");
        Add("AdminSettings.ReferenceVerification.Enabled.Title",
            "Referentieverificatie (werkervaring)",
            "Work experience reference verification",
            "Weryfikacja referencji (doświadczenie)",
            "Verificare referințe (experiență)",
            "التحقق من مراجع الخبرة");
        Add("AdminSettings.ReferenceVerification.Enabled.Desc",
            "Uit = kandidaten zien geen tip, referentieverzoeken of bevestigde competenties. Acceptatie kan Lobsy:ReferenceVerificationEnabled gebruiken.",
            "Off = candidates do not see the tip, reference requests or confirmed competencies. Acceptatie may use Lobsy:ReferenceVerificationEnabled.",
            "Wył. = kandydaci nie widzą tipu, próśb o referencje ani potwierdzonych kompetencji.",
            "Oprit = candidații nu văd sfatul, cererile de referință sau competențele confirmate.",
            "إيقاف = لا يرى المرشحون النصيحة أو طلبات المراجع أو الكفاءات المؤكدة.");
        Add("AdminSettings.TestsV2.Enabled.Title",
            "Quick-Scan v2 (TestsV2)",
            "Quick-Scan v2 (TestsV2)",
            "Quick-Scan v2 (TestsV2)",
            "Quick-Scan v2 (TestsV2)",
            "Quick-Scan v2 (TestsV2)");
        Add("AdminSettings.TestsV2.Enabled.Desc",
            "Aan = nieuwe kandidaten krijgen de uitgebreidere v2-vragensets. Bestaande v1-resultaten blijven ongewijzigd.",
            "On = new candidates get the expanded v2 item sets. Existing v1 results stay unchanged.",
            "Wł. = nowi kandydaci dostają rozszerzone zestawy v2. Istniejące wyniki v1 pozostają.",
            "Pornit = candidații noi primesc seturile v2 extinse. Rezultatele v1 existente rămân.",
            "تشغيل = يحصل المرشحون الجدد على مجموعات v2. نتائج v1 الحالية تبقى.");
        Add("AdminSettings.TestItemExamples.Enabled.Title",
            "Voorbeelden per testvraag",
            "Examples per test item",
            "Przykłady przy pytaniach testu",
            "Exemple per întrebare de test",
            "أمثلة لكل سؤال اختبار");
        Add("AdminSettings.TestItemExamples.Enabled.Desc",
            "Aan = Quick-Scan toont een vast praktijkvoorbeeld per vraag (NL/EN). Diepte-analyse gebruikt dezelfde catalogus.",
            "On = Quick-Scan shows a fixed practice example per item (NL/EN). Deep analysis uses the same catalog.",
            "Wł. = Quick-Scan pokazuje stały przykład praktyczny (NL/EN). Analiza głęboka używa tej samej katalogu.",
            "Pornit = Quick-Scan arată un exemplu fix (NL/EN). Analiza profundă folosește același catalog.",
            "تشغيل = يعرض Quick-Scan مثالاً ثابتاً لكل سؤال (NL/EN). التحليل العميق يستخدم نفس الكatalog.");
        Add("AdminSettings.TestProgressInsight.Enabled.Title",
            "Voortgang & beeld (Quick-Scan)",
            "Progress insight (Quick-Scan)",
            "Wgląd w postęp (Quick-Scan)",
            "Imagine progres (Quick-Scan)",
            "لمحة التقدم (Quick-Scan)");
        Add("AdminSettings.TestProgressInsight.Enabled.Desc",
            "Aan = motivatie per blok van 5 vragen plus kaart “Jouw beeld tot nu toe”.",
            "On = motivation every 5 items plus a “Your picture so far” card.",
            "Wł. = motywacja co 5 pytań i karta „Twój obraz dotąd”.",
            "Pornit = motivație la fiecare 5 întrebări și card „Imaginea ta până acum”.",
            "تشغيل = تحفيز كل 5 أسئلة وبطاقة «صورتك حتى الآن».");
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
        Add("AdminSettings.UwvFeatures.Enabled.Title",
            "UWV werkmap",
            "UWV toolkit",
            "UWV toolkit",
            "UWV toolkit",
            "UWV toolkit");
        Add("AdminSettings.UwvFeatures.Enabled.Desc",
            "Aan = kansberoepen, ondernemerstest, drijfveren en adviseur-deel op echte kandidatenroutes. Uit = alles verborgen.",
            "On = opportunity occupations, entrepreneur check, drivers and adviser share on live candidate routes. Off = hidden.",
            "Wł. = katalog szans, test przedsiębiorcy, motywatory i udostępnianie doradcy na trasach kandydata. Wył. = ukryte.",
            "Pornit = ocupații cu șanse, test antreprenor, motivatori și partajare consilier pe rutele candidatului. Oprit = ascuns.",
            "تشغيل = فرص العمل واختبار ريادة الأعمال والدوافع ومشاركة المستشار على مسارات المرشح. إيقاف = مخفي.");
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
        Add("AdminSettings.CandidateExternalVacancies.Enabled.Title",
            "Externe vacatures",
            "External vacancies",
            "Oferty zewnętrzne",
            "Joburi externe",
            "وظائف خارجية");
        Add("AdminSettings.CandidateExternalVacancies.Enabled.Desc",
            "Kandidaten plakken links van andere vacaturesites en solliciteren via Lobsy.",
            "Candidates paste links from other job sites and apply via Lobsy.",
            "Kandydaci wklejają linki z innych portali i aplikują przez Lobsy.",
            "Candidații lipește linkuri de pe alte site-uri și aplică prin Lobsy.",
            "المرشحون يلصقون روابط من مواقع أخرى ويتقدمون عبر لوبسي.");
        Add("AdminSettings.ProactiveMatching.Enabled.Title",
            "Proactieve matching",
            "Proactive matching",
            "Proaktywne dopasowanie",
            "Potrivire proactivă",
            "المطابقة الاستباقية");
        Add("AdminSettings.ProactiveMatching.Enabled.Desc",
            "Lobsy zoekt voor je, werkgever-shortlist en kennismakingsverzoeken. Standaard uit.",
            "Lobsy searches for you, employer shortlists and introduction requests. Default off.",
            "Lobsy szuka dla Ciebie, shortlisty i prośby o kontakt. Domyślnie wył.",
            "Lobsy caută pentru tine, shortlist-uri și cereri de cunoaștere. Implicit oprit.",
            "لوبسي يبحث لك، قوائم أصحاب العمل وطلبات التعارف. متوقف افتراضياً.");
        Add("AdminSettings.MatchCalibrationV1.Enabled.Title",
            "Matchkalibratie V1",
            "Match calibration V1",
            "Kalibracja dopasowania V1",
            "Calibrare match V1",
            "معايرة المطابقة V1");
        Add("AdminSettings.MatchCalibrationV1.Enabled.Desc",
            "Realistischer matchpercentage: geen volle punten bij ontbrekende data, aparte betrouwbaarheid. Standaard uit.",
            "More realistic match scores: no full credit for missing data, separate confidence. Default off.",
            "Bardziej realistyczny match: brak pełnych punktów przy brakujących danych, osobna pewność. Domyślnie wył.",
            "Match mai realist: fără puncte pline la date lipsă, încredere separată. Implicit oprit.",
            "مطابقة أكثر واقعية: لا نقاط كاملة عند نقص البيانات، ثقة منفصلة. متوقف افتراضياً.");
        Add("Match.Confidence.Indicative",
            "Indicatief — vul je profiel aan voor een betere match.",
            "Indicative — complete your profile for a better match.",
            "Orientacyjnie — uzupełnij profil, aby lepiej dopasować oferty.",
            "Orientativ — completează profilul pentru un match mai bun.",
            "تقريبي — أكمل ملفك للحصول على مطابقة أفضل.");
        Add("Match.Confidence.Level",
            "Betrouwbaarheid van deze match: ongeveer {0}%.",
            "Confidence in this match: about {0}%.",
            "Pewność tego dopasowania: około {0}%.",
            "Încredere în acest match: circa {0}%.",
            "ثقة هذا التطابق: حوالي {0}%.");
    }
}
