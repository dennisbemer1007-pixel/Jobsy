namespace Jobsy.Web.Localization;

public static class UiStringsContextualTips
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = enText;
            ro[key] = enText;
            ar[key] = enText;
        }

        Add("AdminSettings.ContextualTips.Enabled.Title",
            "Tips op het juiste moment",
            "Tips at the right moment");
        Add("AdminSettings.ContextualTips.Enabled.Desc",
            "Toont tips bij gesprek, aanbod, paspoort en carrière, plus de pagina Alle tips onder Reis.",
            "Shows tips at interview, offer, passport and career, plus the All tips page under Journey.");

        Add("Nav.Tips", "Tips", "Tips");
        Add("Tips.Hub.Eyebrow", "Tips van Lobsy", "Tips from Lobsy");
        Add("Tips.Hub.Title", "Tips voor elke stap", "Tips for every step");
        Add("Tips.Hub.Lead",
            "Korte tips voor je zoektocht en je nieuwe baan. Lobsy laat ze ook zelf zien, op het moment dat je ze nodig hebt.",
            "Short tips for your search and your new job. Lobsy also shows them when you need them.");
        Add("Tips.Hub.SearchPlaceholder", "Zoek een tip, bijvoorbeeld proeftijd", "Search for a tip, for example probation period");
        Add("Tips.Hub.NowEyebrow", "Nu voor jou", "For you now");
        Add("Tips.Hub.NowTitle", "Je gesprek op donderdag", "Your interview on Thursday");
        Add("Tips.Hub.NowLead", "Drie korte tips om je rustig voor te bereiden.", "Three short tips to help you prepare calmly.");
        Add("Tips.Hub.NowCta", "Lees de tips", "Read the tips");
        Add("Tips.Hub.SettingsTitle", "Tips op het juiste moment", "Tips at the right moment");
        Add("Tips.Hub.SettingsLead",
            "Lobsy laat een tip zien bij je gesprek, je aanbod, je paspoort en je carrière. Je hebt {0} tips verborgen.",
            "Lobsy shows a tip at your interview, offer, passport and career. You have hidden {0} tips.");
        Add("Tips.Hub.RestoreHidden", "Verborgen tips terugzetten", "Restore hidden tips");
        Add("Tips.Hub.TipCount", "{0} tips", "{0} tips");
        Add("Tips.Hide", "Niet meer tonen", "Do not show again");
        Add("Tips.Eyebrow", "Tip van Lobsy", "Tip from Lobsy");
        Add("Tips.Career.Eyebrow", "Tip voor je volgende stap", "Tip for your next step");

        Add("Tips.Cat.Interview.Title", "Sollicitatiegesprek", "Job interview");
        Add("Tips.Cat.Interview.Lead", "Voorbereiden, vragen stellen en daarna.", "Prepare, ask questions and afterwards.");
        Add("Tips.Cat.Motivation.Title", "Motivatie", "Motivation");
        Add("Tips.Cat.Motivation.Lead", "Laten zien waarom je deze baan wilt.", "Show why you want this job.");
        Add("Tips.Cat.Passport.Title", "CV en paspoort", "CV and passport");
        Add("Tips.Cat.Passport.Lead", "Je ervaring goed laten zien, ook zonder diploma.", "Show your experience well, even without a diploma.");
        Add("Tips.Cat.Contract.Title", "Contract", "Contract");
        Add("Tips.Cat.Contract.Lead", "Proeftijd, cao en wat er in je contract staat.", "Probation, collective agreement and what is in your contract.");
        Add("Tips.Cat.Salary.Title", "Salaris", "Salary");
        Add("Tips.Cat.Salary.Lead", "Wat je verdient, vakantiegeld en toeslagen.", "What you earn, holiday pay and allowances.");
        Add("Tips.Cat.Development.Title", "Ontwikkeling", "Development");
        Add("Tips.Cat.Development.Lead", "Cursussen, groeien en een volgende stap.", "Courses, growth and a next step.");
        Add("Tips.Cat.WorkBenefits.Title", "Werk en uitkering", "Work and benefits");
        Add("Tips.Cat.WorkBenefits.Lead", "Wat er verandert als je weer gaat werken.", "What changes when you start working again.");

        Add("Tips.Placement.InterviewPrep.Title", "Zo bereid je je gesprek voor", "How to prepare for your interview");
        Add("Tips.Placement.InterviewPrep.Step1Title", "Lees de vacature", "Read the vacancy");
        Add("Tips.Placement.InterviewPrep.Step1Body",
            "Kies twee dingen die je leuk lijkt aan het werk. Daar kun je in het gesprek over vertellen.",
            "Pick two things you like about the job. You can talk about them in the interview.");
        Add("Tips.Placement.InterviewPrep.Step2Title", "Kies een voorbeeld", "Pick an example");
        Add("Tips.Placement.InterviewPrep.Step2Body",
            "Wanneer was je zorgvuldig of hielp je een klant goed? Een kort verhaal blijft beter hangen.",
            "When were you careful or did you help a customer well? A short story is easier to remember.");
        Add("Tips.Placement.InterviewPrep.Step3Title", "Plan je reis", "Plan your trip");
        Add("Tips.Placement.InterviewPrep.Step3Body",
            "Kijk hoe je er komt en vertrek op tijd. Dan hoef je niet te haasten.",
            "See how to get there and leave on time. Then you do not have to rush.");
        Add("Tips.Placement.InterviewPrep.Practice", "Oefen je sollicitatiegesprek", "Practise your job interview");
        Add("Tips.Placement.InterviewPrep.AllTips", "Alle gesprektips", "All interview tips");

        Add("Tips.Page.Interview.Title", "Je gesprek staat vast", "Your interview is set");
        Add("Tips.Page.Interview.Lead",
            "Hieronder lees je hoe je je rustig voorbereidt.",
            "Below you can read how to prepare calmly.");

        Add("Tips.Placement.Offer.Title", "Waar let je op?", "What should you check?");
        Add("Tips.Placement.Offer.ContractTitle", "Je contract", "Your contract");
        Add("Tips.Placement.Offer.Contract1", "Kloppen je uren, je loon en je startdatum met wat jullie hebben afgesproken?", "Do your hours, pay and start date match what you agreed?");
        Add("Tips.Placement.Offer.Contract2", "Staat erin hoe lang het contract duurt en wat er daarna gebeurt?", "Does it say how long the contract lasts and what happens next?");
        Add("Tips.Placement.Offer.Contract3", "Snap je iets niet? Vraag het gewoon. Dat is heel normaal.", "Do you not understand something? Just ask. That is very normal.");
        Add("Tips.Placement.Offer.ProbationTitle", "Proeftijd", "Probation period");
        Add("Tips.Placement.Offer.ProbationBody",
            "Bij een contract van 7 maanden mag de proeftijd hooguit 1 maand zijn. Bij een contract van een half jaar of korter mag er geen proeftijd in staan.",
            "With a 7-month contract, probation may be at most 1 month. With six months or less, there should be no probation.");
        Add("Tips.Placement.Offer.CaoTitle", "Cao", "Collective agreement");
        Add("Tips.Placement.Offer.CaoBody",
            "Een cao zijn afspraken voor een hele branche, zoals loon, toeslagen en vrije dagen. Voor jou geldt de cao voor het Bakkersbedrijf.",
            "A collective agreement sets rules for a whole sector, such as pay, allowances and leave.");
        Add("Tips.Placement.Offer.CaoLink", "Wat staat er in deze cao?", "What is in this collective agreement?");
        Add("Tips.Placement.Offer.SalaryTitle", "Je salaris", "Your salary");
        Add("Tips.Placement.Offer.SalaryBody",
            "Dit loon ligt boven het minimumloon. Vakantiegeld komt er nog bij, dat krijg je meestal in mei of juni.",
            "This pay is above minimum wage. Holiday pay comes on top, usually in May or June.");
        Add("Tips.Placement.Offer.Reflection",
            "Je mag om bedenktijd vragen. Een paar dagen is heel gewoon.",
            "You may ask for time to think. A few days is very normal.");
        Add("Tips.Placement.Offer.AllTips", "Alle tips over contract en salaris", "All tips about contract and pay");

        Add("Tips.Page.Offer.Title", "Je krijgt een aanbod", "You received an offer");
        Add("Tips.Page.Offer.Lead",
            "Lees het rustig door. Je hoeft niet meteen te beslissen.",
            "Read it calmly. You do not have to decide right away.");

        Add("Tips.Placement.PassportEducation.Title", "Ook een cursus telt", "A course counts too");
        Add("Tips.Placement.PassportEducation.Body",
            "Geen diploma? Zet er dan ook een cursus, training of certificaat in. Denk aan een hygiënecursus, BHV of een cursus via je werk. Werkgevers zien graag dat je blijft leren.",
            "No diploma? Add a course, training or certificate. Think of hygiene, first aid or training via work. Employers like to see that you keep learning.");
        Add("Tips.Placement.PassportEducation.Cta", "Opleiding of cursus toevoegen", "Add education or course");

        Add("Tips.Placement.CareerDev.Title", "Vraag om een ontwikkelgesprek", "Ask for a development talk");
        Add("Tips.Placement.CareerDev.Body",
            "Vertel je leidinggevende dat je wilt groeien. Veel werkgevers betalen een cursus mee als je erom vraagt. Een goed moment is na je proeftijd.",
            "Tell your manager you want to grow. Many employers pay for a course if you ask. A good moment is after your probation.");
        Add("Tips.Placement.CareerDev.Check1", "Zeg wat je wilt leren, bijvoorbeeld deeg maken of de kassa afsluiten.", "Say what you want to learn, for example making dough or closing the till.");
        Add("Tips.Placement.CareerDev.Check2", "Vraag of er een cursus is die bij het bedrijf past.", "Ask if there is a course that fits the company.");
        Add("Tips.Placement.CareerDev.Check3", "Spreek af wanneer jullie er weer over praten.", "Agree when you will talk about it again.");
        Add("Tips.Placement.CareerDev.Plan", "Zet het in je planning", "Add it to your planning");
        Add("Tips.Placement.CareerDev.More", "Meer over ontwikkelen", "More about development");

        Add("Tips.Detail.FirstInterview.Title", "Je eerste gesprek", "Your first interview");
        Add("Tips.Detail.FirstInterview.Summary",
            "Een gesprek is spannend, zeker de eerste keer. Met deze tips weet je wat je kunt verwachten.",
            "An interview can feel exciting, especially the first time. These tips show what to expect.");
        Add("Tips.Detail.FirstInterview.Block1.Title", "Voor het gesprek", "Before the interview");
        Add("Tips.Detail.FirstInterview.Block1.Body",
            "Lees de vacature nog een keer. Kies twee dingen die je leuk lijkt aan het werk. Bedenk ook een moment waarop je iets goed deed, bijvoorbeeld een klant die blij naar huis ging. Kijk hoe je er komt en vertrek op tijd. Dan hoef je niet te haasten.",
            "Read the vacancy again. Pick two things you like about the job. Think of a moment you did something well. See how to get there and leave on time.");
        Add("Tips.Detail.FirstInterview.Block2.Title", "Tijdens het gesprek", "During the interview");
        Add("Tips.Detail.FirstInterview.Block2.Body",
            "Het gesprek is om elkaar te leren kennen. Jij kijkt ook of het werk bij je past. Vertel gewoon wie je bent. Je hoeft niet perfect te zijn. Snap je een vraag niet? Vraag dan of iemand het nog eens wil zeggen. Dat is helemaal goed.",
            "The interview is to get to know each other. You also check if the job fits you. Just tell who you are. You do not have to be perfect. If you miss a question, ask someone to repeat it.");
        Add("Tips.Detail.FirstInterview.Block3.Title", "Na het gesprek", "After the interview");
        Add("Tips.Detail.FirstInterview.Block3.Body",
            "Je hoort via Lobsy wat de uitkomst is, ook als het nee is. Heb je nog een vraag? Stuur dan een bericht. Ging het niet zoals je wilde? Dat overkomt iedereen. Elk gesprek is oefening voor het volgende.",
            "You will hear the outcome via Lobsy, even when it is a no. Still have a question? Send a message. Did not go as you hoped? That happens to everyone. Each interview is practice for the next.");
        Add("Tips.Detail.FeedbackPrompt", "Had je iets aan deze tip?", "Was this tip helpful?");
        Add("Tips.Detail.FeedbackYes", "Ja, handig", "Yes, helpful");
        Add("Tips.Detail.FeedbackNo", "Niet echt", "Not really");
        Add("Tips.Detail.RelatedTitle", "Meer over gesprekken", "More about interviews");
        Add("Tips.Detail.PracticeTitle", "Oefen eerst even", "Practise first");
        Add("Tips.Detail.PracticeLead", "Met Lobsy, zo vaak als je wilt", "With Lobsy, as often as you like");
        Add("Tips.Detail.PracticeBody",
            "Lobsy stelt je de vragen die een werkgever misschien ook stelt. Je krijgt na elke vraag een korte tip terug.",
            "Lobsy asks questions an employer might ask too. After each question you get a short tip back.");
        Add("Tips.Detail.PracticeCta", "Oefen je sollicitatiegesprek", "Practise your job interview");

        Add("Tips.Reminder.Label", "Herinnering voorbereiding gesprek", "Interview prep reminder");
        Add("Tips.Reminder.Desc",
            "Een dag van tevoren, alleen als je dit zelf aanzet. Standaard uit.",
            "One day before, only if you turn this on. Off by default.");

        Add("Tips.Hub.InterviewAsk.Title", "Welke vragen kun je zelf stellen?", "Which questions can you ask?");
        Add("Tips.Hub.InterviewAsk.Summary",
            "Vraag bijvoorbeeld hoe een gewone werkdag eruitziet, of met wie je samenwerkt.",
            "Ask what a normal workday looks like, or who you work with.");
        Add("Tips.Hub.InterviewOutfit.Title", "Wat trek je aan?", "What should you wear?");
        Add("Tips.Hub.InterviewOutfit.Summary",
            "Kies kleding waarin je je op je gemak voelt en die past bij het bedrijf. Netjes hoeft niet formeel te zijn.",
            "Pick clothes you feel comfortable in that fit the company. Smart does not have to mean formal.");
        Add("Tips.Hub.InterviewCalm.Title", "Zenuwachtig? Dat mag", "Nervous? That is okay");
        Add("Tips.Hub.InterviewCalm.Summary",
            "Adem rustig in en uit. Het gesprek is om elkaar te leren kennen, niet om perfect te zijn.",
            "Breathe slowly. The interview is to get to know each other, not to be perfect.");
        Add("Tips.Hub.InterviewAfter.Title", "Na het gesprek", "After the interview");
        Add("Tips.Hub.InterviewAfter.Summary",
            "Je hoort via Lobsy wat de uitkomst is. Heb je nog een vraag? Stuur dan een bericht.",
            "You will hear the outcome via Lobsy. Still have a question? Send a message.");
        Add("Tips.Hub.MotivationWhy.Title", "Waarom deze baan?", "Why this job?");
        Add("Tips.Hub.MotivationWhy.Summary",
            "Noem iets concreets uit de vacature dat bij je past, bijvoorbeeld werken met mensen of vaste uren.",
            "Mention something concrete from the vacancy that fits you, for example working with people or fixed hours.");
        Add("Tips.Hub.PassportNoDiploma.Title", "Ook zonder diploma", "Even without a diploma");
        Add("Tips.Hub.PassportNoDiploma.Summary",
            "Laat zien wat je wél kunt: werkervaring, cursussen en taken waar je trots op bent.",
            "Show what you can do: work experience, courses and tasks you are proud of.");
        Add("Tips.Hub.ContractProbation.Title", "Proeftijd in je contract", "Probation in your contract");
        Add("Tips.Hub.ContractProbation.Summary",
            "Bij een kort contract mag er vaak geen proeftijd in staan. Twijfel je? Vraag het gewoon.",
            "With a short contract there is often no probation. Not sure? Just ask.");
        Add("Tips.Hub.SalaryMinimum.Title", "Minimumloon", "Minimum wage");
        Add("Tips.Hub.SalaryMinimum.Summary",
            "Je mag niet onder het minimumloon werken. Vakantiegeld en soms toeslagen komen daar nog bij.",
            "You may not work below minimum wage. Holiday pay and sometimes allowances come on top.");
        Add("Tips.Hub.DevelopmentCourse.Title", "Cursus via je werkgever", "Course via your employer");
        Add("Tips.Hub.DevelopmentCourse.Summary",
            "Veel werkgevers betalen een cursus mee als je erom vraagt. Vertel wat je wilt leren.",
            "Many employers pay for a course if you ask. Say what you want to learn.");
        Add("Tips.Hub.WorkBenefitsChange.Title", "Van uitkering naar werk", "From benefits to work");
        Add("Tips.Hub.WorkBenefitsChange.Summary",
            "Als je gaat werken verandert je uitkering. Vraag het UWV of je gemeente wat dit voor jou betekent.",
            "When you start working your benefits change. Ask the UWV or your municipality what this means for you.");

        Add("Tips.Settings.MomentTitle", "Tips op het juiste moment", "Tips at the right moment");
        Add("Tips.Settings.MomentHint",
            "Lobsy laat een tip zien bij je gesprek, aanbod, paspoort en carrière.",
            "Lobsy shows a tip at your interview, offer, passport and career.");
        Add("Tips.Settings.ReminderTitle", "Herinnering gesprek", "Interview reminder");
        Add("Tips.Settings.ReminderHint",
            "Een dag van tevoren, alleen als je dit zelf aanzet.",
            "One day before, only if you turn this on.");
        Add("Tips.Detail.Back", "Terug naar alle tips", "Back to all tips");
        Add("Tips.Detail.NotFound", "Deze tip bestaat niet.", "This tip does not exist.");
        Add("Tips.Detail.SummaryFallback", "Lees de tip hieronder.", "Read the tip below.");
        Add("Tips.Category.AllTips", "Alle tips in deze categorie", "All tips in this category");
        Add("Tips.Apps.LinkGesprek", "Tips voor je gesprek", "Tips for your interview");
        Add("Tips.Apps.LinkAanbod", "Tips bij je aanbod", "Tips for your offer");
        Add("Tips.Apps.InlineGesprek", "Bereid je gesprek voor", "Prepare for your interview");
        Add("Tips.Discovery.LinkTitle", "Tips voor elke stap", "Tips for every step");
        Add("Tips.Discovery.LinkDesc",
            "Korte tips over gesprek, contract, salaris en meer.",
            "Short tips about interviews, contracts, pay and more.");
        Add("Tips.Discovery.LinkCta", "Bekijk alle tips", "See all tips");
        Add("Tips.FeedbackThanks", "Bedankt voor je reactie.", "Thanks for your feedback.");
    }
}
