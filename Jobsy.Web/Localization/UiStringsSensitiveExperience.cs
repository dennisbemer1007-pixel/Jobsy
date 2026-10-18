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
        AddNl(nl);
        AddEn(en);
        foreach (var key in nl.Keys.Where(k => k.StartsWith("Sensitive.", StringComparison.Ordinal)).ToList())
        {
            pl.TryAdd(key, nl[key]);
            ro.TryAdd(key, nl[key]);
            ar.TryAdd(key, nl[key]);
        }
    }

    private static void AddNl(Dictionary<string, string> d)
    {
        d["Sensitive.Work.PrivacyBanner"] =
            "Liever iets niet delen met werkgevers? Dat kan altijd. Het telt wel mee voor je matches.";
        d["Sensitive.Work.OnlyForMe"] = "Alleen voor mij";
        d["Sensitive.Work.OnlyForMeOn"] =
            "Verborgen voor werkgevers. Telt wel mee voor je matches.";
        d["Sensitive.Work.OnlyForMeOff"] =
            "Uit: werkgevers zien deze ervaring in je paspoort.";
        d["Sensitive.Work.SidebarTitle"] = "Zo werkt 'Alleen voor mij'";
        d["Sensitive.Work.SidebarLead"] =
            "Jij bepaalt per ervaring wie hem ziet. Je hoeft niets uit te leggen.";
        d["Sensitive.Work.SidebarHide"] = "Werkgevers zien het niet";
        d["Sensitive.Work.SidebarHideSub"] =
            "Niet in je paspoort, niet in je sollicitaties.";
        d["Sensitive.Work.SidebarMatch"] = "Het telt wel mee";
        d["Sensitive.Work.SidebarMatchSub"] =
            "Wat je ermee kunt, gebruiken we voor je matches.";
        d["Sensitive.Work.SidebarEdit"] = "Altijd aan te passen";
        d["Sensitive.Work.SidebarEditSub"] =
            "Later anders besloten? Zet de switch om in je paspoort.";
        d["Sensitive.Work.SidebarSkillsNote"] =
            "Werkgevers zien wel de vaardigheden die je hebt opgedaan, zonder te zien waar.";
        d["Sensitive.Work.VisibleBadge"] = "Zichtbaar";
        d["Sensitive.Edit.NeutralTitle"] = "Liever een neutrale omschrijving?";
        d["Sensitive.Edit.SkillsTitle"] = "Dit kun je";
        d["Sensitive.Edit.EmployerOptional"] = "Een bedrijfsnaam is niet nodig.";
        d["Sensitive.Edit.YearsOnly"] = "Alleen jaartallen is genoeg.";
        d["Sensitive.Edit.EmployerPreviewTitle"] = "DIT ZIET EEN WERKGEVER";
        d["Sensitive.Edit.EmployerPreviewHidden"] =
            "Geen functienaam, geen periode. Deze ervaring staat op 'Alleen voor mij'.";
        d["Sensitive.Edit.NeutralWhyTitle"] = "Waarom een neutrale naam?";
        d["Sensitive.Edit.NeutralWhy1"] =
            "Lobsy kijkt vooral naar wat je kunt, niet alleen naar je functietitel.";
        d["Sensitive.Edit.NeutralWhy2"] =
            "Een neutrale naam helpt je skills te laten zien zonder details te delen die je liever privé houdt.";
        d["Sensitive.Edit.NeutralWhy3"] =
            "Jij kiest wat werkgevers zien. Verborgen ervaring telt nog steeds mee voor matches.";
        d["Sensitive.Edit.RoleLabel"] = "Hoe wil je dit werk noemen?";
        d["Sensitive.Edit.EmployerLabel"] = "Werkgever";
        d["Sensitive.Edit.PeriodLabel"] = "Periode";
        d["Sensitive.Edit.Chosen"] = "Gekozen";
        d["Sensitive.Gap.Sidebar1"] =
            "Voor periodes die je liever niet invult. Geen tekstvak, geen uitleg aan werkgevers.";
        d["Sensitive.Gap.Sidebar2"] =
            "Het telt niet negatief mee in je matches.";
        d["Sensitive.Gap.Sidebar3"] =
            "Werkgevers zien geen label en geen melding over een gat.";
        d["Sensitive.Ref.WhoAsk"] = "Wie vraag je?";
        d["Sensitive.Ref.Name"] = "Naam";
        d["Sensitive.Ref.Email"] = "E-mailadres";
        d["Sensitive.Ref.Relationship"] = "Waar ken je diegene van?";
        d["Sensitive.Ref.EmployerPreviewTitle"] = "DIT ZIET EEN WERKGEVER";
        d["Sensitive.Ref.EmployerPreviewShort"] =
            "Bevestigd door een begeleider bij de competenties die Fatima herkent. Geen naam, geen organisatie, geen traject.";
        d["Sensitive.Ref.YourReferences"] = "Je referenties";
        d["Sensitive.Ref.StatusConfirmed"] = "Bevestigd";
        d["Sensitive.Ref.StatusPending"] = "Wacht op reactie";
        d["Sensitive.Timeline.Title"] = "Jouw tijdlijn";
        d["Sensitive.Edit.CardTitle"] = "Ervaring bewerken";
        d["Sensitive.Ref.NewTitle"] = "Nieuwe referentie";
        d["Sensitive.Ref.Step"] = "Stap 1 van 2";
        d["Sensitive.Gap.Title"] = "Tussen {0} en {1} staat nog niets";
        d["Sensitive.Gap.OtherPeriod"] = "Andere periode";
        d["Sensitive.Gap.Care"] = "Zorg voor familie of mezelf";
        d["Sensitive.Gap.Education"] = "Opleiding of cursus";
        d["Sensitive.Gap.Leave"] = "Laat zo";
        d["Sensitive.Gap.OtherConfirmed"] =
            "Gemarkeerd als 'Andere periode'. Geen uitleg nodig. Het telt niet negatief mee en werkgevers zien geen label.";
        d["Sensitive.Gap.SidebarTitle"] = "Wat is een 'Andere periode'?";
        d["Sensitive.Gap.EmployerPreviewFooter"] =
            "Gewoon je werk onder elkaar. Zonder label, zonder verborgen ervaring.";
        d["Sensitive.Ref.Title"] = "Wie kan laten zien wat je kunt?";
        d["Sensitive.Ref.Lead"] =
            "Referenties hoeven geen baas te zijn. Iemand die jouw kwaliteiten kent, telt ook mee.";
        d["Sensitive.Ref.PrivacyBanner"] =
            "Jij kiest wie. Werkgevers zien alleen de rol, nooit waar je diegene van kent.";
        d["Sensitive.Ref.TrajectInfo"] =
            "Ook iemand van een traject telt, bijvoorbeeld een coach van een re-integratietraject, of je maatschappelijk werker.";
        d["Sensitive.Ref.RelationshipPrivate"] =
            "Alleen jij en {0} zien dit. Een werkgever niet.";
        d["Sensitive.Ref.EmployerPreview"] =
            "Dit ziet een werkgever: 'Bevestigd door een begeleider' bij de competenties die Fatima herkent. Geen naam, geen organisatie, geen traject.";
        d["Sensitive.Ref.Type.Manager"] = "Leidinggevende";
        d["Sensitive.Ref.Type.Colleague"] = "Collega";
        d["Sensitive.Ref.Type.RegularClient"] = "Vaste klant of opdrachtgever";
        d["Sensitive.Ref.Type.Coach"] = "Coach of begeleider";
        d["Sensitive.Ref.Type.CareWorker"] = "Hulpverlener";
        d["Sensitive.Ref.Type.Teacher"] = "Docent of stagebegeleider";
        d["Sensitive.Passport.MyView"] = "Mijn weergave";
        d["Sensitive.Passport.EmployerView"] = "Werkgeversweergave";
        d["Sensitive.Passport.HeroTitle"] = "Mijn paspoort / Zo ziet een werkgever jou";
        d["Sensitive.Passport.HeroLead"] =
            "Alleen wat jij deelt. Ervaring die alleen voor jou is en je andere periode staan er niet in.";
        d["Sensitive.Passport.OnlyForYouTitle"] = "Alleen voor jou";
        d["Sensitive.Passport.ControlTitle"] = "Jij houdt de regie";
        d["Sensitive.Passport.ControlLead"] =
            "Wil je later toch iets laten zien? Zet de switch om. Je hoeft niemand iets uit te leggen.";
        d["Sensitive.Passport.EditPassport"] = "Paspoort aanpassen";
        d["Sensitive.Passport.NoEmployerGapLabel"] = "Geen label voor werkgevers";
        d["Sensitive.Passport.CountsForMatches"] = "Telt mee voor je matches";
    }

    private static void AddEn(Dictionary<string, string> d)
    {
        d["Sensitive.Work.PrivacyBanner"] =
            "Prefer not to share something with employers? You can. It still counts for your matches.";
        d["Sensitive.Work.OnlyForMe"] = "Only for me";
        d["Sensitive.Work.OnlyForMeOn"] =
            "Hidden from employers. Still counts for your matches.";
        d["Sensitive.Work.OnlyForMeOff"] =
            "Off: employers see this experience on your passport.";
        d["Sensitive.Work.SidebarTitle"] = "How 'Only for me' works";
        d["Sensitive.Work.SidebarLead"] =
            "You decide per experience who sees it. You do not need to explain.";
        d["Sensitive.Work.SidebarHide"] = "Employers do not see it";
        d["Sensitive.Work.SidebarHideSub"] =
            "Not on your passport, not on applications.";
        d["Sensitive.Work.SidebarMatch"] = "It still counts";
        d["Sensitive.Work.SidebarMatchSub"] =
            "We use what you can do for your matches.";
        d["Sensitive.Work.SidebarEdit"] = "Always adjustable";
        d["Sensitive.Work.SidebarEditSub"] =
            "Changed your mind? Toggle it in your passport.";
        d["Sensitive.Work.SidebarSkillsNote"] =
            "Employers do see skills you gained, without seeing where.";
        d["Sensitive.Work.VisibleBadge"] = "Visible";
        d["Sensitive.Edit.NeutralTitle"] = "Prefer a neutral description?";
        d["Sensitive.Edit.SkillsTitle"] = "What you can do";
        d["Sensitive.Edit.EmployerOptional"] = "A company name is not required.";
        d["Sensitive.Edit.YearsOnly"] = "Years alone are enough.";
        d["Sensitive.Edit.EmployerPreviewTitle"] = "WHAT AN EMPLOYER SEES";
        d["Sensitive.Edit.EmployerPreviewHidden"] =
            "No job title, no period. This experience is set to 'Only for me'.";
        d["Sensitive.Edit.NeutralWhyTitle"] = "Why a neutral name?";
        d["Sensitive.Edit.NeutralWhy1"] =
            "Lobsy focuses on what you can do, not only your job title.";
        d["Sensitive.Edit.NeutralWhy2"] =
            "A neutral name helps show your skills without sharing details you prefer to keep private.";
        d["Sensitive.Edit.NeutralWhy3"] =
            "You choose what employers see. Hidden experience still counts for matches.";
        d["Sensitive.Edit.RoleLabel"] = "What do you want to call this work?";
        d["Sensitive.Edit.EmployerLabel"] = "Employer";
        d["Sensitive.Edit.PeriodLabel"] = "Period";
        d["Sensitive.Edit.Chosen"] = "Chosen";
        d["Sensitive.Gap.Sidebar1"] =
            "For periods you prefer not to fill in. No text box, no explanation to employers.";
        d["Sensitive.Gap.Sidebar2"] =
            "It does not count negatively in your matches.";
        d["Sensitive.Gap.Sidebar3"] =
            "Employers see no label and no gap notice.";
        d["Sensitive.Ref.WhoAsk"] = "Who are you asking?";
        d["Sensitive.Ref.Name"] = "Name";
        d["Sensitive.Ref.Email"] = "Email address";
        d["Sensitive.Ref.Relationship"] = "How do you know this person?";
        d["Sensitive.Ref.EmployerPreviewTitle"] = "WHAT AN EMPLOYER SEES";
        d["Sensitive.Ref.EmployerPreviewShort"] =
            "Confirmed by a supervisor next to competencies your referee recognises. No name, no organisation, no programme.";
        d["Sensitive.Ref.YourReferences"] = "Your references";
        d["Sensitive.Ref.StatusConfirmed"] = "Confirmed";
        d["Sensitive.Ref.StatusPending"] = "Waiting for response";
        d["Sensitive.Timeline.Title"] = "Your timeline";
        d["Sensitive.Edit.CardTitle"] = "Edit experience";
        d["Sensitive.Ref.NewTitle"] = "New reference";
        d["Sensitive.Ref.Step"] = "Step 1 of 2";
        d["Sensitive.Gap.Title"] = "Nothing listed between {0} and {1}";
        d["Sensitive.Gap.OtherPeriod"] = "Other period";
        d["Sensitive.Gap.Care"] = "Care for family or myself";
        d["Sensitive.Gap.Education"] = "Training or course";
        d["Sensitive.Gap.Leave"] = "Leave as is";
        d["Sensitive.Gap.OtherConfirmed"] =
            "Marked as 'Other period'. No explanation needed. It does not count negatively and employers see no label.";
        d["Sensitive.Gap.SidebarTitle"] = "What is an 'Other period'?";
        d["Sensitive.Gap.EmployerPreviewFooter"] =
            "Just your jobs listed. No labels, no hidden experience.";
        d["Sensitive.Ref.Title"] = "Who can show what you can do?";
        d["Sensitive.Ref.Lead"] =
            "References do not have to be a boss. Someone who knows your strengths counts too.";
        d["Sensitive.Ref.PrivacyBanner"] =
            "You choose who. Employers only see the role, never how you know the person.";
        d["Sensitive.Ref.TrajectInfo"] =
            "Someone from a programme counts too, for example a coach from a reintegration programme, or your social worker.";
        d["Sensitive.Ref.RelationshipPrivate"] =
            "Only you and {0} see this. Not an employer.";
        d["Sensitive.Ref.EmployerPreview"] =
            "What an employer sees: 'Confirmed by a supervisor' next to competencies your referee recognises. No name, no organisation, no programme.";
        d["Sensitive.Ref.Type.Manager"] = "Manager";
        d["Sensitive.Ref.Type.Colleague"] = "Colleague";
        d["Sensitive.Ref.Type.RegularClient"] = "Regular client or customer";
        d["Sensitive.Ref.Type.Coach"] = "Coach or supervisor";
        d["Sensitive.Ref.Type.CareWorker"] = "Care worker";
        d["Sensitive.Ref.Type.Teacher"] = "Teacher or internship supervisor";
        d["Sensitive.Passport.MyView"] = "My view";
        d["Sensitive.Passport.EmployerView"] = "Employer view";
        d["Sensitive.Passport.HeroTitle"] = "My passport / How an employer sees you";
        d["Sensitive.Passport.HeroLead"] =
            "Only what you share. Experience that is only for you and your other periods are not included.";
        d["Sensitive.Passport.OnlyForYouTitle"] = "Only for you";
        d["Sensitive.Passport.ControlTitle"] = "You stay in control";
        d["Sensitive.Passport.ControlLead"] =
            "Want to show something later? Flip the switch. You do not owe anyone an explanation.";
        d["Sensitive.Passport.EditPassport"] = "Edit passport";
        d["Sensitive.Passport.NoEmployerGapLabel"] = "No label for employers";
        d["Sensitive.Passport.CountsForMatches"] = "Counts for your matches";
    }
}
