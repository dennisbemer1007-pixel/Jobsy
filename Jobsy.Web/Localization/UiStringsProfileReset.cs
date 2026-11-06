namespace Jobsy.Web.Localization;

internal static class UiStringsProfileReset
{
    internal static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        static void Add(Dictionary<string, string> map, string key, string value) => map[key] = value;

        Add(nl, "Profile.DangerZone.Title", "Gegevens en account");
        Add(en, "Profile.DangerZone.Title", "Data and account");
        Add(pl, "Profile.DangerZone.Title", "Dane i konto");
        Add(ro, "Profile.DangerZone.Title", "Date și cont");
        Add(ar, "Profile.DangerZone.Title", "البيانات والحساب");

        Add(nl, "Profile.Reset.Title", "Begin opnieuw");
        Add(en, "Profile.Reset.Title", "Start over");
        Add(pl, "Profile.Reset.Title", "Zacznij od nowa");
        Add(ro, "Profile.Reset.Title", "Începe din nou");
        Add(ar, "Profile.Reset.Title", "ابدأ من جديد");

        Add(nl, "Profile.Reset.Lead",
            "Wis al je kandidaatgegevens en start opnieuw bij de intro. Je login blijft werken.");
        Add(en, "Profile.Reset.Lead",
            "Clear all your candidate data and start again at the intro. Your login stays active.");
        Add(pl, "Profile.Reset.Lead",
            "Usuń wszystkie dane kandydata i zacznij od nowa od wprowadzenia. Logowanie zostaje.");
        Add(ro, "Profile.Reset.Lead",
            "Șterge toate datele de candidat și începe din nou de la intro. Autentificarea rămâne.");
        Add(ar, "Profile.Reset.Lead",
            "امسح كل بيانات المرشح وابدأ من جديد من المقدمة. تبقى بيانات تسجيل الدخول.");

        Add(nl, "Profile.Reset.Button", "Begin opnieuw");
        Add(en, "Profile.Reset.Button", "Start over");
        Add(pl, "Profile.Reset.Button", "Zacznij od nowa");
        Add(ro, "Profile.Reset.Button", "Începe din nou");
        Add(ar, "Profile.Reset.Button", "ابدأ من جديد");

        Add(nl, "Profile.DangerZone.Divider", "Of");
        Add(en, "Profile.DangerZone.Divider", "Or");
        Add(pl, "Profile.DangerZone.Divider", "Lub");
        Add(ro, "Profile.DangerZone.Divider", "Sau");
        Add(ar, "Profile.DangerZone.Divider", "أو");

        Add(nl, "Profile.Reset.Dialog.Title", "Profiel resetten?");
        Add(en, "Profile.Reset.Dialog.Title", "Reset your profile?");
        Add(pl, "Profile.Reset.Dialog.Title", "Zresetować profil?");
        Add(ro, "Profile.Reset.Dialog.Title", "Resetezi profilul?");
        Add(ar, "Profile.Reset.Dialog.Title", "إعادة تعيين الملف؟");

        Add(nl, "Profile.Reset.Dialog.Lead",
            "Dit wist onomkeerbaar: onboarding, persoonlijke gegevens (behalve login), voorkeuren en beschikbaarheid, werkstatus, alle test- en assessmentresultaten (quick scan, verdieping, diepteanalyse), DNA-paspoort, gegenereerde PDF’s en cv’s, uploads, matchscores, bewaarde en verborgen vacatures, likes en dislikes, sollicitatieconcepten, en je voortgang in de ontdekkingsreis.");
        Add(en, "Profile.Reset.Dialog.Lead",
            "This permanently clears: onboarding, personal data (except login), preferences and availability, work status, all test and assessment results (quick scan, deep dive, deep analysis), DNA passport, generated PDFs and CVs, uploads, match scores, saved and hidden jobs, likes and dislikes, application drafts, and your discovery journey progress.");
        Add(pl, "Profile.Reset.Dialog.Lead",
            "To trwale usuwa: onboarding, dane osobowe (oprócz logowania), preferencje i dostępność, status pracy, wyniki testów i ocen, paszport DNA, PDF-y i CV, uploady, wyniki dopasowania, zapisane ukryte oferty, polubienia, szkice aplikacji i postęp w podróży odkrywczej.");
        Add(ro, "Profile.Reset.Dialog.Lead",
            "Aceasta șterge definitiv: onboarding, date personale (exceptând login), preferințe și disponibilitate, status muncă, rezultate teste, pașaport ADN, PDF-uri și CV-uri, încărcări, scoruri, joburi salvate/ascunse, like/dislike, ciorne și progresul călătoriei de descoperire.");
        Add(ar, "Profile.Reset.Dialog.Lead",
            "يمسح نهائياً: الإعداد، البيانات الشخصية (ما عدا تسجيل الدخول)، التفضيلات والتوفر، حالة العمل، نتائج الاختبارات، جواز DNA، ملفات PDF والسيرة، الرفوعات، النقاط، الوظائف المحفوظة/المخفية، الإعجابات، المسودات، وتقدم رحلة الاكتشاف.");

        Add(nl, "Profile.Reset.Dialog.ApplicationsNote",
            "Sollicitaties die al zijn verstuurd, worden losgekoppeld en geanonimiseerd voor werkgevers; zij kunnen de gegevens die ze al ontvingen niet terugdraaien.");
        Add(en, "Profile.Reset.Dialog.ApplicationsNote",
            "Applications already sent are unlinked and anonymised for employers; data they already received cannot be undone.");
        Add(pl, "Profile.Reset.Dialog.ApplicationsNote",
            "Wysłane aplikacje są odłączane i anonimizowane dla pracodawców; otrzymanych danych nie cofniemy.");
        Add(ro, "Profile.Reset.Dialog.ApplicationsNote",
            "Candidaturile trimise sunt decuplate și anonimizate pentru angajatori; datele deja primite nu pot fi anulate.");
        Add(ar, "Profile.Reset.Dialog.ApplicationsNote",
            "الطلبات المرسلة تُفصل وتُ anonymize للجهات؛ البيانات المستلمة لا يمكن التراجع عنها.");

        Add(nl, "Profile.Reset.Dialog.ResearchNote",
            "Je onderzoekstoestemming wordt ingetrokken. Niet-geëxporteerde onderzoeksdata wordt verwijderd. Anonieme data die al in een export zat, kan niet uit die export worden gehaald.");
        Add(en, "Profile.Reset.Dialog.ResearchNote",
            "Your research consent is withdrawn. Non-exported research data is deleted. Anonymised data already included in an export cannot be removed from that export.");
        Add(pl, "Profile.Reset.Dialog.ResearchNote",
            "Zgoda na badania zostaje cofnięta. Nieeksportowane dane badań są usuwane. Zanonimizowane dane już w eksporcie nie da się stamtąd usunąć.");
        Add(ro, "Profile.Reset.Dialog.ResearchNote",
            "Consimțământul pentru cercetare este retras. Datele neexportate sunt șterse. Datele anonimizate deja exportate nu pot fi scoase din export.");
        Add(ar, "Profile.Reset.Dialog.ResearchNote",
            "يُسحب موافقة البحث. تُحذف البيانات غير المُصدَّرة. لا يمكن إزالة البيانات المجهولة التي دخلت تصديراً سابقاً من ذلك التصدير.");

        Add(nl, "Profile.Reset.Dialog.ConfirmLabel", "Typ {0} om te bevestigen");
        Add(en, "Profile.Reset.Dialog.ConfirmLabel", "Type {0} to confirm");
        Add(pl, "Profile.Reset.Dialog.ConfirmLabel", "Wpisz {0}, aby potwierdzić");
        Add(ro, "Profile.Reset.Dialog.ConfirmLabel", "Scrie {0} pentru confirmare");
        Add(ar, "Profile.Reset.Dialog.ConfirmLabel", "اكتب {0} للتأكيد");

        Add(nl, "Profile.Reset.Dialog.Submit", "Ja, wis mijn profiel");
        Add(en, "Profile.Reset.Dialog.Submit", "Yes, clear my profile");
        Add(pl, "Profile.Reset.Dialog.Submit", "Tak, wyczyść profil");
        Add(ro, "Profile.Reset.Dialog.Submit", "Da, șterge profilul");
        Add(ar, "Profile.Reset.Dialog.Submit", "نعم، امسح ملفي");

        Add(nl, "Profile.Reset.Dialog.Cancel", "Annuleren");
        Add(en, "Profile.Reset.Dialog.Cancel", "Cancel");
        Add(pl, "Profile.Reset.Dialog.Cancel", "Anuluj");
        Add(ro, "Profile.Reset.Dialog.Cancel", "Anulează");
        Add(ar, "Profile.Reset.Dialog.Cancel", "إلغاء");

        Add(nl, "Profile.Reset.Done", "Je profiel is gewist. Je kunt opnieuw beginnen.");
        Add(en, "Profile.Reset.Done", "Your profile was cleared. You can start again.");
        Add(pl, "Profile.Reset.Done", "Profil wyczyszczony. Możesz zacząć od nowa.");
        Add(ro, "Profile.Reset.Done", "Profilul a fost șters. Poți începe din nou.");
        Add(ar, "Profile.Reset.Done", "تم مسح ملفك. يمكنك البدء من جديد.");
    }
}
