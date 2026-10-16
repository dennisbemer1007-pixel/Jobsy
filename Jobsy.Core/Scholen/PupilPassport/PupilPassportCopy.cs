using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>NL copy for leerling DNA-paspoort v2 (tips, discovery, cards). Merged into UiStrings via PupilVerhaalCopy prefix.</summary>
public static class PupilPassportCopy
{
    private static readonly IReadOnlyDictionary<string, string> Strings = Build();

    public static string Get(string key) => Strings.TryGetValue(key, out var v) ? v : key;

    public static string JobDomain(string jobKey)
    {
        var letter = JobRiasecLetter(jobKey);
        return Get($"LeerlingPassport.Card.Domain.{letter}");
    }

    public static string JobBlurb(string jobKey, PupilQuestionSet set)
    {
        var specific = $"LeerlingPassport.Card.{jobKey}.Blurb";
        if (Strings.TryGetValue(specific, out var blurb))
        {
            return blurb;
        }

        return Get(set == PupilQuestionSet.Groep78
            ? "LeerlingPassport.Card.Blurb.Generic.G78"
            : "LeerlingPassport.Card.Blurb.Generic.Vo");
    }

    public static IReadOnlyList<string> DiscoveryQuestions(string? dreamJobKey, string? hollandCode)
    {
        if (!string.IsNullOrWhiteSpace(dreamJobKey)
            && !string.Equals(dreamJobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            var list = new List<string>();
            for (var i = 1; i <= 3; i++)
            {
                var key = $"LeerlingPassport.Discover.{dreamJobKey}.{i}";
                if (Strings.TryGetValue(key, out var q))
                {
                    list.Add(q);
                }
            }

            if (list.Count >= 2)
            {
                return list;
            }
        }

        var letter = string.IsNullOrWhiteSpace(hollandCode) ? 'S' : char.ToUpperInvariant(hollandCode[0]);
        return
        [
            Get($"LeerlingPassport.Discover.Letter.{letter}.1"),
            Get($"LeerlingPassport.Discover.Letter.{letter}.2"),
            Get($"LeerlingPassport.Discover.Letter.{letter}.3")
        ];
    }

    public static IReadOnlyList<string> HomeClassTips(string? dreamJobKey, char riasecLetter)
    {
        if (!string.IsNullOrWhiteSpace(dreamJobKey)
            && Strings.TryGetValue($"LeerlingPassport.Tip.{dreamJobKey}.1", out var t1))
        {
            var tips = new List<string> { t1 };
            if (Strings.TryGetValue($"LeerlingPassport.Tip.{dreamJobKey}.2", out var t2))
            {
                tips.Add(t2);
            }

            return tips;
        }

        return
        [
            Get($"LeerlingPassport.Tip.Letter.{riasecLetter}.1"),
            Get("LeerlingPassport.Tip.Generic.2")
        ];
    }

    public static string ArchetypeTagline(string? hollandCode)
    {
        if (string.IsNullOrWhiteSpace(hollandCode) || hollandCode.Length < 2)
        {
            return Get("LeerlingPassport.Vo.Archetype.Default");
        }

        var pair = $"{char.ToUpperInvariant(hollandCode[0])}{char.ToUpperInvariant(hollandCode[1])}";
        return Get($"LeerlingPassport.Vo.Archetype.{pair}");
    }

    private static char JobRiasecLetter(string jobKey)
    {
        foreach (var (letter, jobs) in PupilRiasecJobIdeas.ByLetter)
        {
            if (jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                return letter;
            }
        }

        return 'S';
    }

    private static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        d["LeerlingPassport.Header.G78"] = "MIJN DNA-PASPOORT";
        d["LeerlingPassport.Header.Vo"] = "DNA-paspoort · middelbare school";
        d["LeerlingPassport.Stamp"] = "{0}/{1} werelden ontdekt";
        d["LeerlingPassport.NameIntro"] = "Dit paspoort is van";
        d["LeerlingPassport.NamePrivacy"] = "je naam schrijf je zelf op · Lobsy bewaart geen namen";
        d["LeerlingPassport.Section.DitBenIk"] = "Dit ben ik!";
        d["LeerlingPassport.Section.Droom"] = "Hier droom ik van…";
        d["LeerlingPassport.Section.Likes"] = "Jaaa, leuk!";
        d["LeerlingPassport.Section.Dislikes"] = "Liever niet";
        d["LeerlingPassport.OwnWord"] = "mijn eigen woord:";
        d["LeerlingPassport.Section.Strengths"] = "Hier ben ik goed in";
        d["LeerlingPassport.Strengths.Sub"] = "zo zie ik mezelf";
        d["LeerlingPassport.Section.Cards"] = "Mijn beroepenkaarten";
        d["LeerlingPassport.Cards.Sub"] = "om te ontdekken · geen vacatures";
        d["LeerlingPassport.Cards.Fit"] = "Past bij";
        d["LeerlingPassport.Section.Discover"] = "Dit wil ik nog ontdekken";
        d["LeerlingPassport.Section.Tip"] = "Tip voor thuis en in de klas";
        d["LeerlingPassport.Footer.G78"] =
            "Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken, voor jou, je ouders en je leraar.\nLobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.";
        d["LeerlingPassport.Footer.Vo"] =
            "Geen cijfer en geen schooladvies. Dit paspoort is om te ontdekken.\nLobsy bewaart geen namen. Je antwoorden worden na dit schooljaar gewist.";

        d["LeerlingPassport.Strength.Samenwerken.Sub"] = "ik luister en denk mee";
        d["LeerlingPassport.Strength.Zorgvuldig.Sub"] = "ik doe dingen netjes";
        d["LeerlingPassport.Strength.Helpen.Sub"] = "ik zie het als iemand vastzit";
        d["LeerlingPassport.Strength.Onderzoek.Sub"] = "ik wil weten hoe het zit";
        d["LeerlingPassport.Strength.Praktisch.Sub"] = "ik pak dingen graag zelf aan";
        d["LeerlingPassport.Strength.Resultaat.Sub"] = "ik werk doelgericht";
        d["LeerlingPassport.Strength.Stress.Sub"] = "ik blijf kalm als het druk is";
        d["LeerlingPassport.Strength.Innovatie.Sub"] = "ik bedenk graag nieuwe dingen";
        d["LeerlingPassport.Strength.Extraversie.Sub"] = "ik praat graag met anderen";
        d["LeerlingPassport.Strength.Verbinding.Sub"] = "ik werk het liefst samen";
        d["LeerlingPassport.Strength.Doelen.Sub"] = "ik houd van een duidelijk doel";
        d["LeerlingPassport.Strength.Impact.Sub"] = "ik wil iets betekenen voor anderen";
        d["LeerlingPassport.Strength.Betrouwbaar.Sub"] = "afspraken kan je op me rekenen";

        d["LeerlingPassport.Card.Domain.R"] = "Buiten & techniek";
        d["LeerlingPassport.Card.Domain.I"] = "Onderzoek";
        d["LeerlingPassport.Card.Domain.A"] = "Creatief";
        d["LeerlingPassport.Card.Domain.S"] = "Zorg & dieren";
        d["LeerlingPassport.Card.Domain.E"] = "Ondernemen";
        d["LeerlingPassport.Card.Domain.C"] = "Precies & netjes";
        d["LeerlingPassport.Card.Blurb.Generic.G78"] = "Een beroep om over na te denken — geen vacature, wel een idee om verder te ontdekken.";
        d["LeerlingPassport.Card.Blurb.Generic.Vo"] = "Past bij jouw interesses — om verder te verkennen, geen vacature.";

        d["LeerlingPassport.Card.dierenverzorger.Blurb"] =
            "Je zorgt elke dag voor dieren: eten geven, verzorgen en kijken of ze gezond zijn.";
        d["LeerlingPassport.Card.dierenarts.Blurb"] =
            "Je helpt dieren beter te worden. Je onderzoekt wat er mis is en werkt met mensen die zorgen.";
        d["LeerlingPassport.Card.bioloog.Blurb"] =
            "Je onderzoekt hoe planten en dieren leven, buiten in de natuur of in een lab.";
        d["LeerlingPassport.Card.hovenier.Blurb"] =
            "Je ontwerpt en maakt tuinen en parken. Je bent bijna de hele dag buiten.";

        d["LeerlingPassport.Discover.dierenarts.1"] = "Hoe ziet een dag bij de dierenarts eruit?";
        d["LeerlingPassport.Discover.dierenarts.2"] = "Welke vakken horen bij biologie op de middelbare school?";
        d["LeerlingPassport.Discover.dierenarts.3"] = "Ben ik ook goed in iets maken met mijn handen?";
        d["LeerlingPassport.Tip.dierenarts.1"] =
            "Ga samen naar een kinderboerderij of dierenasiel en vraag wat de verzorgers doen.";
        d["LeerlingPassport.Tip.dierenarts.2"] = "Vraag wat het leukst was. Er is geen goed of fout!";

        d["LeerlingPassport.Discover.Letter.S.1"] = "Bij welk beroep help je anderen het meest?";
        d["LeerlingPassport.Discover.Letter.S.2"] = "Wanneer voel jij je het meest nuttig?";
        d["LeerlingPassport.Discover.Letter.S.3"] = "Welke stage of uitje past bij zorg of dieren?";
        d["LeerlingPassport.Discover.Letter.I.1"] = "Welk vraagstuk zou jij willen uitzoeken?";
        d["LeerlingPassport.Discover.Letter.I.2"] = "Welke proef of onderzoeksopdracht vond je leuk?";
        d["LeerlingPassport.Discover.Letter.I.3"] = "Welk vak geeft je het meeste 'weten hoe het zit'?";
        d["LeerlingPassport.Tip.Letter.S.1"] = "Praat thuis over momenten waarop je iemand hielp — wat deed je precies?";
        d["LeerlingPassport.Tip.Generic.2"] = "Er is geen goed of fout antwoord; het gaat om ontdekken.";

        d["LeerlingPassport.Vo.Archetype.Default"] = "Nieuwsgierige ontdekker — leert door te proberen";
        d["LeerlingPassport.Vo.Archetype.SI"] = "Zorgzame onderzoeker — wil weten hoe het zit én helpt graag";
        d["LeerlingPassport.Vo.Section.Interests"] = "Mijn interesses";
        d["LeerlingPassport.Vo.Likes"] = "Dit vind ik leuk";
        d["LeerlingPassport.Vo.Dislikes"] = "Liever niet";
        d["LeerlingPassport.Vo.DnaTitle"] = "Mijn DNA in 4 lagen";
        d["LeerlingPassport.Vo.Strengths"] = "Sterke punten";
        d["LeerlingPassport.Vo.Sectors"] = "Sectoren die bij mij passen";
        d["LeerlingPassport.Vo.SideJobs"] = "Bijbanen en stages die bij mij passen";
        d["LeerlingPassport.Vo.SideJobs.Sub"] = "types werk · geen vacatures";
        d["LeerlingPassport.Vo.Dream"] = "Droombaan en studierichting";
        d["LeerlingPassport.Vo.Learn"] = "Wat ik wil leren";
        d["LeerlingPassport.Vo.OwnWords"] = "In mijn eigen woorden";
        d["LeerlingPassport.Vo.OwnWords.Note"] = "Geschreven door de leerling · niet aangepast door Lobsy";
        d["LeerlingPassport.Vo.Derived"] = "AFGELEID";
        d["LeerlingPassport.Vo.NamePrompt"] = "Naam op je paspoort (optioneel, alleen op de PDF)";

        d["LeerlingPassport.Vo.SideJob.kinderboerderij.Title"] = "Hulp op een kinderboerderij";
        d["LeerlingPassport.Vo.SideJob.kinderboerderij.Sub"] = "dieren & buiten";
        d["LeerlingPassport.Vo.SideJob.kinderboerderij.Why"] = "Past bij zorgzaam zijn en dieren — rustig meelopen en vragen stellen.";
        d["LeerlingPassport.Vo.SideJob.dierenkliniek-snuffel.Title"] = "Snuffelstage dierenkliniek";
        d["LeerlingPassport.Vo.SideJob.dierenkliniek-snuffel.Sub"] = "kort kijken · geen vacature";
        d["LeerlingPassport.Vo.SideJob.dierenkliniek-snuffel.Why"] = "Om te ontdekken hoe een dierenarts werkt — vaak vanaf 15 jaar met begeleiding.";
        d["LeerlingPassport.Vo.SideJob.drogisterij-vakken.Title"] = "Vakkenvuller drogisterij";
        d["LeerlingPassport.Vo.SideJob.drogisterij-vakken.Sub"] = "netjes & zorgvuldig";
        d["LeerlingPassport.Vo.SideJob.drogisterij-vakken.Why"] = "Leuk als je van ordenen houdt; let op tijden (tot 19:00 als je 15 bent).";
        d["LeerlingPassport.Vo.SideJob.Meta.Weekend"] = "zaterdag";
        d["LeerlingPassport.Vo.SideJob.Meta.FromAge"] = "vanaf {0}";

        foreach (var (letter, title) in new (string, string)[]
                 {
                     ("S", "Zorg & welzijn"), ("I", "Onderzoek & lab"), ("A", "Creatief & media"),
                     ("R", "Techniek & buiten"), ("E", "Handel & organisatie"), ("C", "Administratie & zorgvuldig werk")
                 })
        {
            d[$"LeerlingPassport.Vo.Sector.{letter}.Title"] = title;
            d[$"LeerlingPassport.Vo.Sector.{letter}.B1"] = "Past bij hoe jij bent in de test";
            d[$"LeerlingPassport.Vo.Sector.{letter}.B2"] = "Sluit aan bij je interesses";
            d[$"LeerlingPassport.Vo.Sector.{letter}.B3"] = "Om verder te verkennen — geen vacature";
            d[$"LeerlingPassport.Vo.Sector.{letter}.Role1"] = "Voorbeeldberoep A";
            d[$"LeerlingPassport.Vo.Sector.{letter}.Role2"] = "Voorbeeldberoep B";
        }

        return d;
    }
}
