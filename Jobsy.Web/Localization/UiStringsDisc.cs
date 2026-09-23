using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

internal static class UiStringsDisc
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, Nl());
        Merge(en, En());
        Merge(pl, Nl());
        Merge(ro, Nl());
        Merge(ar, Nl());
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> extra)
    {
        foreach (var (key, value) in extra)
        {
            target[key] = value;
        }
    }

    private static Dictionary<string, string> Nl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kompas.TabDisc"] = "DISC-Analyse",
        ["Kompas.DiscLead"] = "Je gedragsstijl in het team: hoe jij het voortouw neemt, mensen meeneemt, ritme houdt en nauwkeurig werkt. Open een stijl voor ontwikkelpunten en workshops.",
        ["Disc.Title"] = "Gedragsanalyse",
        ["Disc.Lead"] = "Vijfentwintig korte stellingen (ca. 3 minuten) over hoe jij je gedraagt op de werkvloer. Daarna zie je je primaire stijl — en workshops om te groeien.",
        ["Disc.ScienceNote"] = "De vragen gaan over dagelijks gedrag: tempo, contact, ritme en precisie. Geen diagnose, wel een helder startpunt voor matching.",
        ["Disc.PrivacyNote"] = "Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["Disc.Empty"] = "Je hebt de gedragsanalyse nog niet ingevuld. In ongeveer drie minuten zie je hoe jij in een team werkt.",
        ["Disc.Start"] = "Start de Quick-Scan",
        ["Disc.Continue"] = "Quick-Scan verder invullen",
        ["Disc.Retake"] = "Quick-Scan opnieuw invullen / aanpassen",
        ["Disc.SavedComplete"] = "Gedragsanalyse opgeslagen. Je scores en matches zijn bijgewerkt.",
        ["Disc.DeepUnlock"] = "Ontgrendel diepte-analyse (€ 2,99)",
        ["Disc.RadarLabel"] = "Verdeling van je vier gedragsstijlen",
        ["Disc.TrainingTitle"] = "Workshops en cursussen",
        ["Deep.DiscTitle"] = "Uitgebreide gedragsanalyse",
        ["Disc.Cat.Dominant"] = "Dominant — het voortouw",
        ["Disc.Cat.Dominant.Hint"] = "Tempo, besluiten en richting geven als het werk vastloopt.",
        ["Disc.Cat.Invloed"] = "Invloedrijk — mensen meenemen",
        ["Disc.Cat.Invloed.Hint"] = "Contact, sfeer en anderen meekrijgen in het werk.",
        ["Disc.Cat.Stabiel"] = "Stabiel — rust en ritme",
        ["Disc.Cat.Stabiel.Hint"] = "Voorspelbaar werk, volhouden en het team steunen.",
        ["Disc.Cat.Nauwkeurig"] = "Consciëntieus — nauwkeurig",
        ["Disc.Cat.Nauwkeurig.Hint"] = "Checken, afronden en volgens afspraak werken.",
        ["Disc.Cat.Dominant.MeaningHigh"] = "Jij pakt het voortouw. Teams met tempo — logistiek, pieken in de kas of een volle winkelavond — benutten dat.",
        ["Disc.Cat.Dominant.MeaningMid"] = "Je kunt sturen als het moet, en kunt nog groeien in snelle knopen doorhakken.",
        ["Disc.Cat.Dominant.MeaningLow"] = "Leiding nemen kost je nog energie. Een korte workshop besluiten onder druk helpt.",
        ["Disc.Cat.Dominant.Develop"] = "Onder druk: oefen één duidelijke prioriteit per shift. Communicatie: zeg wat het doel is voordat het overleg uitdijt.",
        ["Disc.Cat.Invloed.MeaningHigh"] = "Jij neemt mensen mee. Retail, horeca en inwerken van seizoenscollega’s liggen je.",
        ["Disc.Cat.Invloed.MeaningMid"] = "Je kunt contact maken, en kunt dat nog vaker inzetten als de sfeer hangt.",
        ["Disc.Cat.Invloed.MeaningLow"] = "Veel praten op een dag is vermoeiend. Een workshop klantgesprek of presenteren helpt in kleine stappen.",
        ["Disc.Cat.Invloed.Develop"] = "Teamrol: nodig één stille collega uit om iets te zeggen. Communicatie: oefen een kort, warm welkom.",
        ["Disc.Cat.Stabiel.MeaningHigh"] = "Jij houdt ritme. Kas, zorg, vaste magazijnronden of een voorspelbare ploeg passen sterk.",
        ["Disc.Cat.Stabiel.MeaningMid"] = "Je kunt volhouden, en kunt nog groeien in een vast anker zijn voor wisselende ploegen.",
        ["Disc.Cat.Stabiel.MeaningLow"] = "Steeds hetzelfde ritme verveelt snel. Kies werk met afwisseling, of oefen één vaste ronde per dag.",
        ["Disc.Cat.Stabiel.Develop"] = "Onder druk: houd één ronde gelijk, ook als anderen racen. Teamrol: wees de overdracht die klopt.",
        ["Disc.Cat.Nauwkeurig.MeaningHigh"] = "Jij checkt en rondt netjes af. Kwaliteit, registratie en inpakken in de keten benutten dat.",
        ["Disc.Cat.Nauwkeurig.MeaningMid"] = "Je wilt het goed doen, en kunt nog scherper volgens de lijst werken als het druk is.",
        ["Disc.Cat.Nauwkeurig.MeaningLow"] = "Lijsten en checks voelen als oponthoud. Een korte cursus kwaliteit of administratie maakt het lichter.",
        ["Disc.Cat.Nauwkeurig.Develop"] = "Onder druk: vink twee must-checks af vóór tempo. Communicatie: meld een fout open, niet achteraf.",
        ["Disc.Q01"] = "Ik neem het voortouw als het werk stilvalt.",
        ["Disc.Q02"] = "Ik zeg duidelijk wat er eerst moet als het druk wordt.",
        ["Disc.Q03"] = "Ik durf een knoop door te hakken als twee werkwijzen botsen.",
        ["Disc.Q04"] = "Ik zet tempo als een deadline krap is.",
        ["Disc.Q05"] = "Ik stuur bij als de ploeg zonder plan staat.",
        ["Disc.Q06"] = "Ik wacht tot een ander het besluit neemt als het hectisch is.",
        ["Disc.Q07"] = "Ik houd me afzijdig als er leiding nodig is.",
        ["Disc.Q08"] = "Ik krijg energie van contact met klanten of collega’s.",
        ["Disc.Q09"] = "Ik neem mensen mee in een verhaal als de briefing droog is.",
        ["Disc.Q10"] = "Ik maak contact als een nieuwe collega verloren kijkt.",
        ["Disc.Q11"] = "Ik houd de sfeer luchtig als de shift lang duurt.",
        ["Disc.Q12"] = "Ik werk het liefst zonder extra praatjes als het druk is.",
        ["Disc.Q13"] = "Ik laat anderen het woord doen in overleg.",
        ["Disc.Q14"] = "Ik houd van een vast ritme van week tot week.",
        ["Disc.Q15"] = "Collega’s kunnen op mij rekenen tot het einde van de shift.",
        ["Disc.Q16"] = "Ik werk het liefst met duidelijke, terugkerende ronden.",
        ["Disc.Q17"] = "Ik steun het team als het seizoen piekt.",
        ["Disc.Q18"] = "Ik wissel graag steeds van taak, ook als een vaste ronde beter is.",
        ["Disc.Q19"] = "Ik raak ongeduldig van hetzelfde ritme elke dag.",
        ["Disc.Q20"] = "Ik controleer de lijst voordat ik iets afrond.",
        ["Disc.Q21"] = "Ik werk netjes volgens de afgesproken stappen.",
        ["Disc.Q22"] = "Ik meld een fout in plaats van die weg te moffelen.",
        ["Disc.Q23"] = "Ik maak de administratie van mijn shift kloppend voordat ik ga.",
        ["Disc.Q24"] = "Ik sla stappen over als er haast is.",
        ["Disc.Q25"] = "Ik vul checks later wel in als het druk is."
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kompas.TabDisc"] = "DISC analysis",
        ["Kompas.DiscLead"] = "Your team behaviour: taking the lead, bringing people along, keeping rhythm and working accurately. Open a style for growth points and workshops.",
        ["Disc.Title"] = "Behaviour analysis",
        ["Disc.Lead"] = "Twenty-five short statements (about 3 minutes) about how you behave at work. Then you see your primary style — and workshops to grow.",
        ["Disc.ScienceNote"] = "The questions are about daily behaviour: pace, contact, rhythm and precision. Not a diagnosis — a clear starting point for matching.",
        ["Disc.PrivacyNote"] = "Your answers stay in your account. Employers never see raw answers. Export and delete via My data. More in the",
        ["Disc.Empty"] = "You have not taken the behaviour analysis yet. In about three minutes you see how you work in a team.",
        ["Disc.Start"] = "Start the Quick-Scan",
        ["Disc.Continue"] = "Continue the Quick-Scan",
        ["Disc.Retake"] = "Retake / edit the Quick-Scan",
        ["Disc.SavedComplete"] = "Behaviour analysis saved. Your scores and matches have been updated.",
        ["Disc.DeepUnlock"] = "Unlock deep analysis (€ 2.99)",
        ["Disc.RadarLabel"] = "Breakdown of your four behaviour styles",
        ["Disc.TrainingTitle"] = "Workshops and courses",
        ["Deep.DiscTitle"] = "Extended behaviour analysis",
        ["Disc.Cat.Dominant"] = "Dominant — taking the lead",
        ["Disc.Cat.Dominant.Hint"] = "Pace, decisions and direction when work stalls.",
        ["Disc.Cat.Invloed"] = "Influential — bringing people along",
        ["Disc.Cat.Invloed.Hint"] = "Contact, atmosphere and getting others on board.",
        ["Disc.Cat.Stabiel"] = "Steady — calm and rhythm",
        ["Disc.Cat.Stabiel.Hint"] = "Predictable work, persistence and supporting the team.",
        ["Disc.Cat.Nauwkeurig"] = "Conscientious — accurate",
        ["Disc.Cat.Nauwkeurig.Hint"] = "Checking, finishing and working to the agreement.",
        ["Disc.Cat.Dominant.MeaningHigh"] = "You take the lead. Teams with pace — logistics, harvest peaks or a busy shop evening — use that.",
        ["Disc.Cat.Dominant.MeaningMid"] = "You can steer when needed, and can still grow in making fast calls.",
        ["Disc.Cat.Dominant.MeaningLow"] = "Taking the lead still costs energy. A short workshop on decisions under pressure helps.",
        ["Disc.Cat.Dominant.Develop"] = "Under pressure: practise one clear priority per shift. Communication: say the goal before the meeting expands.",
        ["Disc.Cat.Invloed.MeaningHigh"] = "You bring people along. Retail, hospitality and onboarding seasonal colleagues suit you.",
        ["Disc.Cat.Invloed.MeaningMid"] = "You can make contact, and can use that more when the mood drops.",
        ["Disc.Cat.Invloed.MeaningLow"] = "A lot of talking in a day is tiring. A workshop on customer talks or presenting helps in small steps.",
        ["Disc.Cat.Invloed.Develop"] = "Team role: invite one quiet colleague to speak. Communication: practise a short, warm welcome.",
        ["Disc.Cat.Stabiel.MeaningHigh"] = "You keep rhythm. Greenhouse, care, warehouse rounds or a predictable team fit strongly.",
        ["Disc.Cat.Stabiel.MeaningMid"] = "You can persist, and can still grow as a steady anchor for changing crews.",
        ["Disc.Cat.Stabiel.MeaningLow"] = "The same rhythm bores you quickly. Choose more variety, or practise one fixed round a day.",
        ["Disc.Cat.Stabiel.Develop"] = "Under pressure: keep one round steady even if others race. Team role: be the handover that is correct.",
        ["Disc.Cat.Nauwkeurig.MeaningHigh"] = "You check and finish neatly. Quality, registration and packing in the chain use that.",
        ["Disc.Cat.Nauwkeurig.MeaningMid"] = "You want to do it well, and can still work more to the list when it is busy.",
        ["Disc.Cat.Nauwkeurig.MeaningLow"] = "Lists and checks feel like delay. A short quality or admin course makes it lighter.",
        ["Disc.Cat.Nauwkeurig.Develop"] = "Under pressure: tick two must-checks before speeding up. Communication: report a mistake openly, not afterwards.",
        ["Disc.Q01"] = "I take the lead when the work stalls.",
        ["Disc.Q02"] = "I say clearly what comes first when it gets busy.",
        ["Disc.Q03"] = "I dare to decide when two ways of working clash.",
        ["Disc.Q04"] = "I pick up the pace when a deadline is tight.",
        ["Disc.Q05"] = "I steer when the crew has no plan.",
        ["Disc.Q06"] = "I wait for someone else to decide when it is hectic.",
        ["Disc.Q07"] = "I stay on the sidelines when leadership is needed.",
        ["Disc.Q08"] = "I get energy from contact with customers or colleagues.",
        ["Disc.Q09"] = "I bring people along in a story when the briefing is dry.",
        ["Disc.Q10"] = "I make contact when a new colleague looks lost.",
        ["Disc.Q11"] = "I keep the mood light when the shift runs long.",
        ["Disc.Q12"] = "I prefer working without extra chat when it is busy.",
        ["Disc.Q13"] = "I let others do the talking in meetings.",
        ["Disc.Q14"] = "I like a steady rhythm from week to week.",
        ["Disc.Q15"] = "Colleagues can count on me until the end of the shift.",
        ["Disc.Q16"] = "I prefer clear, repeating rounds.",
        ["Disc.Q17"] = "I support the team when the season peaks.",
        ["Disc.Q18"] = "I like switching tasks even when a fixed round is better.",
        ["Disc.Q19"] = "I get impatient with the same rhythm every day.",
        ["Disc.Q20"] = "I check the list before I finish something.",
        ["Disc.Q21"] = "I work neatly to the agreed steps.",
        ["Disc.Q22"] = "I report a mistake instead of hiding it.",
        ["Disc.Q23"] = "I make the shift admin add up before I leave.",
        ["Disc.Q24"] = "I skip steps when there is a rush.",
        ["Disc.Q25"] = "I fill in checks later when it is busy."
    };
}
