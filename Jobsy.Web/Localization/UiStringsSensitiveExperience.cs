namespace Jobsy.Web.Localization;

public static class UiStringsSensitiveExperience
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

        Add("Sensitive.Work.PrivacyBanner",
            "Liever iets niet delen met werkgevers? Dat kan altijd. Het telt wel mee voor je matches.",
            "Prefer not to share something with employers? You can. It still counts for your matches.",
            "Prefer not to share something with employers? You can. It still counts for your matches.",
            "Prefer not to share something with employers? You can. It still counts for your matches.",
            "Prefer not to share something with employers? You can. It still counts for your matches.");
        Add("AdminSettings.SensitiveExperience.Enabled.Title", "Gevoelige werkervaring", "Sensitive work history", "Wrażliwe doświadczenie zawodowe", "Experiență de lucru sensibilă", "خبرة عمل حساسة");
        Add("AdminSettings.SensitiveExperience.Enabled.Desc", "Privacy voor gevoelige werkervaring: alleen voor mij, neutrale titels en aparte periodes.", "Privacy for sensitive work history: only for me, neutral titles and separate periods.", "Prywatność wrażliwego doświadczenia: tylko dla mnie, neutralne tytuły i osobne okresy.", "Confidențialitate pentru experiență sensibilă: doar pentru mine, titluri neutre și perioade separate.", "خصوصية خبرة العمل الحساسة: لي فقط، عناوين محايدة وفترات منفصلة.");
        Add("Sensitive.Work.OnlyForMe", "Alleen voor mij", "Only for me", "Only for me", "Only for me", "Only for me");
        Add("Sensitive.Work.OnlyForMeOn",
            "Verborgen voor werkgevers. Telt wel mee voor je matches.",
            "Hidden from employers. Still counts for your matches.",
            "Hidden from employers. Still counts for your matches.",
            "Hidden from employers. Still counts for your matches.",
            "Hidden from employers. Still counts for your matches.");
        Add("Sensitive.Work.OnlyForMeOff", "Uit: werkgevers zien deze ervaring in je paspoort.", "Off: employers see this experience on your passport.", "Off: employers see this experience on your passport.", "Off: employers see this experience on your passport.", "Off: employers see this experience on your passport.");
        Add("Sensitive.Work.SidebarTitle", "Zo werkt 'Alleen voor mij'", "How 'Only for me' works", "How 'Only for me' works", "How 'Only for me' works", "How 'Only for me' works");
        Add("Sensitive.Work.SidebarLead",
            "Jij bepaalt per ervaring wie hem ziet. Je hoeft niets uit te leggen.",
            "You decide per experience who sees it. You do not need to explain.",
            "You decide per experience who sees it. You do not need to explain.",
            "You decide per experience who sees it. You do not need to explain.",
            "You decide per experience who sees it. You do not need to explain.");
        Add("Sensitive.Work.SidebarHide", "Werkgevers zien het niet", "Employers do not see it", "Employers do not see it", "Employers do not see it", "Employers do not see it");
        Add("Sensitive.Work.SidebarHideSub", "Niet in je paspoort, niet in je sollicitaties.", "Not on your passport, not on applications.", "Not on your passport, not on applications.", "Not on your passport, not on applications.", "Not on your passport, not on applications.");
        Add("Sensitive.Work.SidebarMatch", "Het telt wel mee", "It still counts", "It still counts", "It still counts", "It still counts");
        Add("Sensitive.Work.SidebarMatchSub", "Wat je ermee kunt, gebruiken we voor je matches.", "We use what you can do for your matches.", "We use what you can do for your matches.", "We use what you can do for your matches.", "We use what you can do for your matches.");
        Add("Sensitive.Work.SidebarEdit", "Altijd aan te passen", "Always adjustable", "Always adjustable", "Always adjustable", "Always adjustable");
        Add("Sensitive.Work.SidebarEditSub",
            "Later anders besloten? Zet de switch om in je paspoort.",
            "Changed your mind? Toggle it in your passport.",
            "Changed your mind? Toggle it in your passport.",
            "Changed your mind? Toggle it in your passport.",
            "Changed your mind? Toggle it in your passport.");
        Add("Sensitive.Work.SidebarSkillsNote",
            "Werkgevers zien wel de vaardigheden die je hebt opgedaan, zonder te zien waar.",
            "Employers do see skills you gained, without seeing where.",
            "Employers do see skills you gained, without seeing where.",
            "Employers do see skills you gained, without seeing where.",
            "Employers do see skills you gained, without seeing where.");
        Add("Sensitive.Work.VisibleBadge", "Zichtbaar", "Visible", "Visible", "Visible", "Visible");
        Add("Sensitive.Edit.NeutralTitle", "Liever een neutrale omschrijving?", "Prefer a neutral description?", "Prefer a neutral description?", "Prefer a neutral description?", "Prefer a neutral description?");
        Add("Sensitive.Edit.SkillsTitle", "Dit kun je", "What you can do", "What you can do", "What you can do", "What you can do");
        Add("Sensitive.Edit.EmployerOptional", "Een bedrijfsnaam is niet nodig.", "A company name is not required.", "A company name is not required.", "A company name is not required.", "A company name is not required.");
        Add("Sensitive.Edit.YearsOnly", "Alleen jaartallen is genoeg.", "Years alone are enough.", "Years alone are enough.", "Years alone are enough.", "Years alone are enough.");
        Add("Sensitive.Edit.EmployerPreviewTitle", "DIT ZIET EEN WERKGEVER", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES");
        Add("Sensitive.Edit.EmployerPreviewHidden",
            "Geen functienaam, geen periode. Deze ervaring staat op 'Alleen voor mij'.",
            "No job title, no period. This experience is set to 'Only for me'.",
            "No job title, no period. This experience is set to 'Only for me'.",
            "No job title, no period. This experience is set to 'Only for me'.",
            "No job title, no period. This experience is set to 'Only for me'.");
        Add("Sensitive.Edit.NeutralWhyTitle", "Waarom een neutrale naam?", "Why a neutral name?", "Why a neutral name?", "Why a neutral name?", "Why a neutral name?");
        Add("Sensitive.Edit.NeutralWhy1",
            "Lobsy kijkt vooral naar wat je kunt, niet alleen naar je functietitel.",
            "Lobsy focuses on what you can do, not only your job title.",
            "Lobsy focuses on what you can do, not only your job title.",
            "Lobsy focuses on what you can do, not only your job title.",
            "Lobsy focuses on what you can do, not only your job title.");
        Add("Sensitive.Edit.NeutralWhy2",
            "Een neutrale naam helpt je skills te laten zien zonder details te delen die je liever privé houdt.",
            "A neutral name helps show your skills without sharing details you prefer to keep private.",
            "A neutral name helps show your skills without sharing details you prefer to keep private.",
            "A neutral name helps show your skills without sharing details you prefer to keep private.",
            "A neutral name helps show your skills without sharing details you prefer to keep private.");
        Add("Sensitive.Edit.NeutralWhy3",
            "Jij kiest wat werkgevers zien. Verborgen ervaring telt nog steeds mee voor matches.",
            "You choose what employers see. Hidden experience still counts for matches.",
            "You choose what employers see. Hidden experience still counts for matches.",
            "You choose what employers see. Hidden experience still counts for matches.",
            "You choose what employers see. Hidden experience still counts for matches.");
        Add("Sensitive.Edit.RoleLabel", "Hoe wil je dit werk noemen?", "What do you want to call this work?", "What do you want to call this work?", "What do you want to call this work?", "What do you want to call this work?");
        Add("Sensitive.Edit.EmployerLabel", "Werkgever", "Employer", "Employer", "Employer", "Employer");
        Add("Sensitive.Edit.PeriodLabel", "Periode", "Period", "Period", "Period", "Period");
        Add("Sensitive.Edit.Chosen", "Gekozen", "Chosen", "Chosen", "Chosen", "Chosen");
        Add("Sensitive.Gap.Sidebar1",
            "Voor periodes die je liever niet invult. Geen tekstvak, geen uitleg aan werkgevers.",
            "For periods you prefer not to fill in. No text box, no explanation to employers.",
            "For periods you prefer not to fill in. No text box, no explanation to employers.",
            "For periods you prefer not to fill in. No text box, no explanation to employers.",
            "For periods you prefer not to fill in. No text box, no explanation to employers.");
        Add("Sensitive.Gap.Sidebar2", "Het telt niet negatief mee in je matches.", "It does not count negatively in your matches.", "It does not count negatively in your matches.", "It does not count negatively in your matches.", "It does not count negatively in your matches.");
        Add("Sensitive.Gap.Sidebar3",
            "Werkgevers zien geen label en geen melding over een gat.",
            "Employers see no label and no gap notice.",
            "Employers see no label and no gap notice.",
            "Employers see no label and no gap notice.",
            "Employers see no label and no gap notice.");
        Add("Sensitive.Ref.WhoAsk", "Wie vraag je?", "Who are you asking?", "Who are you asking?", "Who are you asking?", "Who are you asking?");
        Add("Sensitive.Ref.Name", "Naam", "Name", "Name", "Name", "Name");
        Add("Sensitive.Ref.Email", "E-mailadres", "Email address", "Email address", "Email address", "Email address");
        Add("Sensitive.Ref.Relationship", "Waar ken je diegene van?", "How do you know this person?", "How do you know this person?", "How do you know this person?", "How do you know this person?");
        Add("Sensitive.Ref.EmployerPreviewTitle", "DIT ZIET EEN WERKGEVER", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES", "WHAT AN EMPLOYER SEES");
        Add("Sensitive.Ref.EmployerPreviewShort",
            "Bevestigd door een begeleider bij de vaardigheden die Fatima herkent. Geen naam, geen organisatie, geen traject.",
            "Confirmed by a supervisor next to competencies your referee recognises. No name, no organisation, no programme.",
            "Confirmed by a supervisor next to competencies your referee recognises. No name, no organisation, no programme.",
            "Confirmed by a supervisor next to competencies your referee recognises. No name, no organisation, no programme.",
            "Confirmed by a supervisor next to competencies your referee recognises. No name, no organisation, no programme.");
        Add("Sensitive.Ref.YourReferences", "Je referenties", "Your references", "Your references", "Your references", "Your references");
        Add("Sensitive.Ref.StatusConfirmed", "Bevestigd", "Confirmed", "Confirmed", "Confirmed", "Confirmed");
        Add("Sensitive.Ref.StatusPending", "Wacht op reactie", "Waiting for response", "Waiting for response", "Waiting for response", "Waiting for response");
        Add("Sensitive.Timeline.Title", "Jouw tijdlijn", "Your timeline", "Your timeline", "Your timeline", "Your timeline");
        Add("Sensitive.Edit.CardTitle", "Ervaring bewerken", "Edit experience", "Edit experience", "Edit experience", "Edit experience");
        Add("Sensitive.Ref.NewTitle", "Nieuwe referentie", "New reference", "New reference", "New reference", "New reference");
        Add("Sensitive.Ref.Step", "Stap 1 van 2", "Step 1 of 2", "Step 1 of 2", "Step 1 of 2", "Step 1 of 2");
        Add("Sensitive.Gap.Title", "Tussen {0} en {1} staat nog niets", "Nothing listed between {0} and {1}", "Nothing listed between {0} and {1}", "Nothing listed between {0} and {1}", "Nothing listed between {0} and {1}");
        Add("Sensitive.Gap.OtherPeriod", "Andere periode", "Other period", "Other period", "Other period", "Other period");
        Add("Sensitive.Gap.Care", "Zorg voor familie of mezelf", "Care for family or myself", "Care for family or myself", "Care for family or myself", "Care for family or myself");
        Add("Sensitive.Gap.Education", "Opleiding of cursus", "Training or course", "Training or course", "Training or course", "Training or course");
        Add("Sensitive.Gap.Leave", "Laat zo", "Leave as is", "Leave as is", "Leave as is", "Leave as is");
        Add("Sensitive.Gap.OtherConfirmed",
            "Gemarkeerd als 'Andere periode'. Geen uitleg nodig. Het telt niet negatief mee en werkgevers zien geen label.",
            "Marked as 'Other period'. No explanation needed. It does not count negatively and employers see no label.",
            "Marked as 'Other period'. No explanation needed. It does not count negatively and employers see no label.",
            "Marked as 'Other period'. No explanation needed. It does not count negatively and employers see no label.",
            "Marked as 'Other period'. No explanation needed. It does not count negatively and employers see no label.");
        Add("Sensitive.Gap.SidebarTitle", "Wat is een 'Andere periode'?", "What is an 'Other period'?", "What is an 'Other period'?", "What is an 'Other period'?", "What is an 'Other period'?");
        Add("Sensitive.Gap.EmployerPreviewFooter",
            "Gewoon je werk onder elkaar. Zonder label, zonder verborgen ervaring.",
            "Just your jobs listed. No labels, no hidden experience.",
            "Just your jobs listed. No labels, no hidden experience.",
            "Just your jobs listed. No labels, no hidden experience.",
            "Just your jobs listed. No labels, no hidden experience.");
        Add("Sensitive.Ref.Title", "Wie kan laten zien wat je kunt?", "Who can show what you can do?", "Who can show what you can do?", "Who can show what you can do?", "Who can show what you can do?");
        Add("Sensitive.Ref.Lead",
            "Referenties hoeven geen baas te zijn. Iemand die jouw kwaliteiten kent, telt ook mee.",
            "References do not have to be a boss. Someone who knows your strengths counts too.",
            "References do not have to be a boss. Someone who knows your strengths counts too.",
            "References do not have to be a boss. Someone who knows your strengths counts too.",
            "References do not have to be a boss. Someone who knows your strengths counts too.");
        Add("Sensitive.Ref.PrivacyBanner",
            "Jij kiest wie. Werkgevers zien alleen de rol, nooit waar je diegene van kent.",
            "You choose who. Employers only see the role, never how you know the person.",
            "You choose who. Employers only see the role, never how you know the person.",
            "You choose who. Employers only see the role, never how you know the person.",
            "You choose who. Employers only see the role, never how you know the person.");
        Add("Sensitive.Ref.TrajectInfo",
            "Ook iemand van een traject telt, bijvoorbeeld een coach van een re-integratietraject, of je maatschappelijk werker.",
            "Someone from a programme counts too, for example a coach from a reintegration programme, or your social worker.",
            "Someone from a programme counts too, for example a coach from a reintegration programme, or your social worker.",
            "Someone from a programme counts too, for example a coach from a reintegration programme, or your social worker.",
            "Someone from a programme counts too, for example a coach from a reintegration programme, or your social worker.");
        Add("Sensitive.Ref.RelationshipPrivate", "Alleen jij en {0} zien dit. Een werkgever niet.", "Only you and {0} see this. Not an employer.", "Only you and {0} see this. Not an employer.", "Only you and {0} see this. Not an employer.", "Only you and {0} see this. Not an employer.");
        Add("Sensitive.Ref.EmployerPreview",
            "Dit ziet een werkgever: 'Bevestigd door een begeleider' bij de vaardigheden die Fatima herkent. Geen naam, geen organisatie, geen traject.",
            "What an employer sees: 'Confirmed by a supervisor' next to competencies your referee recognises. No name, no organisation, no programme.",
            "What an employer sees: 'Confirmed by a supervisor' next to competencies your referee recognises. No name, no organisation, no programme.",
            "What an employer sees: 'Confirmed by a supervisor' next to competencies your referee recognises. No name, no organisation, no programme.",
            "What an employer sees: 'Confirmed by a supervisor' next to competencies your referee recognises. No name, no organisation, no programme.");
        Add("Sensitive.Ref.Type.Manager", "Leidinggevende", "Manager", "Manager", "Manager", "Manager");
        Add("Sensitive.Ref.Type.Colleague", "Collega", "Colleague", "Colleague", "Colleague", "Colleague");
        Add("Sensitive.Ref.Type.RegularClient", "Vaste klant of opdrachtgever", "Regular client or customer", "Regular client or customer", "Regular client or customer", "Regular client or customer");
        Add("Sensitive.Ref.Type.Coach", "Coach of begeleider", "Coach or supervisor", "Coach or supervisor", "Coach or supervisor", "Coach or supervisor");
        Add("Sensitive.Ref.Type.CareWorker", "Hulpverlener", "Care worker", "Care worker", "Care worker", "Care worker");
        Add("Sensitive.Ref.Type.Teacher", "Docent of stagebegeleider", "Teacher or internship supervisor", "Teacher or internship supervisor", "Teacher or internship supervisor", "Teacher or internship supervisor");
        Add("Sensitive.Passport.MyView", "Mijn weergave", "My view", "My view", "My view", "My view");
        Add("Sensitive.Passport.EmployerView", "Werkgeversweergave", "Employer view", "Employer view", "Employer view", "Employer view");
        Add("Sensitive.Passport.HeroTitle", "Mijn paspoort / Zo ziet een werkgever jou", "My passport / How an employer sees you", "My passport / How an employer sees you", "My passport / How an employer sees you", "My passport / How an employer sees you");
        Add("Sensitive.Passport.HeroLead",
            "Alleen wat jij deelt. Ervaring die alleen voor jou is en je andere periode staan er niet in.",
            "Only what you share. Experience that is only for you and your other periods are not included.",
            "Only what you share. Experience that is only for you and your other periods are not included.",
            "Only what you share. Experience that is only for you and your other periods are not included.",
            "Only what you share. Experience that is only for you and your other periods are not included.");
        Add("Sensitive.Passport.OnlyForYouTitle", "Alleen voor jou", "Only for you", "Only for you", "Only for you", "Only for you");
        Add("Sensitive.Passport.ControlTitle", "Jij houdt de regie", "You stay in control", "You stay in control", "You stay in control", "You stay in control");
        Add("Sensitive.Passport.ControlLead",
            "Wil je later toch iets laten zien? Zet de switch om. Je hoeft niemand iets uit te leggen.",
            "Want to show something later? Flip the switch. You do not owe anyone an explanation.",
            "Want to show something later? Flip the switch. You do not owe anyone an explanation.",
            "Want to show something later? Flip the switch. You do not owe anyone an explanation.",
            "Want to show something later? Flip the switch. You do not owe anyone an explanation.");
        Add("Sensitive.Passport.EditPassport", "Paspoort aanpassen", "Edit passport", "Edit passport", "Edit passport", "Edit passport");
        Add("Sensitive.Passport.NoEmployerGapLabel", "Geen label voor werkgevers", "No label for employers", "No label for employers", "No label for employers", "No label for employers");
        Add("Sensitive.Passport.CountsForMatches", "Telt mee voor je matches", "Counts for your matches", "Counts for your matches", "Counts for your matches", "Counts for your matches");
    }
}
