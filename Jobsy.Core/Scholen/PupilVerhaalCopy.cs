using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Dutch copy for pupil story + dream-job templates (D12).
/// Single reviewable source; <c>UiStringsLeerlingVerhaal</c> merges these keys.
/// </summary>
public static class PupilVerhaalCopy
{
    public static IReadOnlyDictionary<string, string> All { get; } = Build();

    public static string Get(string key)
        => TryResolve(key, out var v) ? v : key;

    /// <summary>VO uses <c>key.Vo</c> when that variant exists; G78 keeps the base key.</summary>
    public static string Get(string key, PupilQuestionSet set)
    {
        if (set == PupilQuestionSet.Vo)
        {
            var voKey = key + ".Vo";
            if (All.TryGetValue(voKey, out var vo))
            {
                return vo;
            }
        }

        return Get(key);
    }

    public static string Get(string key, PupilClassContext ctx)
    {
        var levelKey = key + LevelSuffix(ctx.Level);
        if (levelKey != key && All.TryGetValue(levelKey, out var levelText))
        {
            return levelText;
        }

        return Get(key, ctx.Set);
    }

    private static string LevelSuffix(SchoolLevel level) => level switch
    {
        SchoolLevel.Havo => ".Havo",
        SchoolLevel.Vwo => ".Vwo",
        SchoolLevel.Groep78 => ".G78",
        SchoolLevel.Mavo => ".Mavo",
        SchoolLevel.VmboB or SchoolLevel.VmboK or SchoolLevel.VmboGt => ".Vmbo",
        _ => ""
    };

    public static bool TryGet(string key, out string value)
        => TryResolve(key, out value);

    /// <summary>
    /// Pair texts are stored once. CI and IC share a line. Any two-letter RIASEC segment
    /// (class prompts, tiles, sentences) tries the alphabetical pair, then the swapped pair,
    /// then the first letter, so a raw key never reaches the screen.
    /// </summary>
    private static bool TryResolve(string key, out string value)
    {
        if (All.TryGetValue(key, out value!))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            value = "";
            return false;
        }

        if (TryRewritePairSegment(key, out value!))
        {
            return true;
        }

        if (TryFirstLetterFallback(key, out value!))
        {
            return true;
        }

        value = "";
        return false;
    }

    private static bool TryFirstLetterFallback(string key, out string value)
    {
        string[] prefixes =
        [
            "LeerlingStory.Class.",
            "LeerlingStory.Tile.Riasec.",
            "LeerlingStory.Riasec."
        ];
        foreach (var prefix in prefixes)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rest = key[prefix.Length..];
            if (rest.Length < 1 || !IsRiasec(rest[0]))
            {
                continue;
            }

            var dot = rest.IndexOf('.');
            var tail = dot >= 0 ? rest[dot..] : "";
            var candidate = prefix + char.ToUpperInvariant(rest[0]) + tail;
            if (!string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase)
                && All.TryGetValue(candidate, out value!))
            {
                return true;
            }
        }

        value = "";
        return false;
    }

    private static bool TryRewritePairSegment(string key, out string value)
    {
        var parts = key.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            var seg = parts[i];
            if (seg.Length != 2
                || !IsRiasec(seg[0])
                || !IsRiasec(seg[1])
                || char.ToUpperInvariant(seg[0]) == char.ToUpperInvariant(seg[1]))
            {
                continue;
            }

            var canonical = PupilStoryTemplates.CanonicalRiasecPair(seg[0], seg[1]);
            var swapped = string.Concat(canonical[1], canonical[0]);
            var first = char.ToUpperInvariant(seg[0]).ToString();
            foreach (var replacement in new[] { canonical, swapped, first })
            {
                var previous = parts[i];
                parts[i] = replacement;
                if (All.TryGetValue(string.Join('.', parts), out value!))
                {
                    return true;
                }

                parts[i] = previous;
            }
        }

        value = "";
        return false;
    }

    private static bool IsRiasec(char letter)
        => "RIASEC".Contains(char.ToUpperInvariant(letter));

    private static Dictionary<string, string> Build()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        d["LeerlingStory.Bf.Samenwerken"] = "Jij bent iemand die goed op anderen let.";
        d["LeerlingStory.Bf.Resultaatgerichtheid"] = "Jij maakt graag af wat je begint.";
        d["LeerlingStory.Bf.Stressbestendigheid"] = "Jij blijft rustig als het even spannend wordt.";
        d["LeerlingStory.Bf.Innovatie"] = "Jij bedenkt graag een nieuwe manier.";
        d["LeerlingStory.Bf.Extraversie"] = "Jij krijgt energie van mensen om je heen.";
        d["LeerlingStory.Bf.Balanced"] = "Jij bent in evenwicht: soms stil, soms actief.";
        d["LeerlingStory.Riasec.R"] = "Met je handen maak je iets.";
        d["LeerlingStory.Riasec.I"] = "Je wilt graag weten hoe iets werkt.";
        d["LeerlingStory.Riasec.A"] = "Je bedenkt graag iets nieuws of moois.";
        d["LeerlingStory.Riasec.S"] = "Je helpt graag andere mensen of dieren.";
        d["LeerlingStory.Riasec.E"] = "Je neemt graag het voortouw in een groep.";
        d["LeerlingStory.Riasec.C"] = "Je houdt van nette plannen en overzicht.";
        d["LeerlingStory.Riasec.AI"] = "Je bedenkt graag iets nieuws en zoekt uit hoe het werkt.";
        d["LeerlingStory.Riasec.AR"] = "Met je handen maak je iets moois.";
        d["LeerlingStory.Riasec.AS"] = "Je helpt graag en bedenkt iets creatiefs.";
        d["LeerlingStory.Riasec.AE"] = "Je bedenkt ideeën en neemt graag het voortouw.";
        d["LeerlingStory.Riasec.AC"] = "Je maakt iets moois en houdt van nette afspraken.";
        d["LeerlingStory.Riasec.IR"] = "Je wilt weten hoe iets werkt en maakt het graag zelf.";
        d["LeerlingStory.Riasec.IS"] = "Je zoekt uit hoe iets werkt en helpt anderen graag.";
        d["LeerlingStory.Riasec.IE"] = "Je zoekt dingen uit en neemt graag het voortouw.";
        d["LeerlingStory.Riasec.IC"] = "Je zoekt precies uit hoe iets werkt en houdt overzicht.";
        d["LeerlingStory.Riasec.RS"] = "Je helpt graag en maakt graag iets met je handen.";
        d["LeerlingStory.Riasec.RE"] = "Je pakt dingen aan en neemt graag het voortouw.";
        d["LeerlingStory.Riasec.RC"] = "Met je handen maak je iets en je houdt van nette stappen.";
        d["LeerlingStory.Riasec.SE"] = "Je helpt graag en neemt het voortouw in de groep.";
        d["LeerlingStory.Riasec.SC"] = "Je helpt graag en houdt van nette afspraken.";
        d["LeerlingStory.Riasec.CE"] = "Je houdt overzicht en neemt graag het voortouw.";
        // Same sentence for both letter orders. The lookup sorts the pair, these keys are the sorted form.
        d["LeerlingStory.Riasec.CI"] = "Je zoekt precies uit hoe iets werkt en houdt overzicht.";
        d["LeerlingStory.Riasec.CR"] = "Met je handen maak je iets en je houdt van nette stappen.";
        d["LeerlingStory.Riasec.CS"] = "Je helpt graag en houdt van nette afspraken.";
        d["LeerlingStory.Riasec.EI"] = "Je zoekt dingen uit en neemt graag het voortouw.";
        d["LeerlingStory.Riasec.ER"] = "Je pakt dingen aan en neemt graag het voortouw.";
        d["LeerlingStory.Riasec.ES"] = "Je helpt graag en neemt het voortouw in de groep.";
        d["LeerlingStory.Val.Autonomy"] = "Belangrijk voor jou: zelf kiezen wat je doet.";
        d["LeerlingStory.Val.Connection"] = "Belangrijk voor jou: samen met anderen.";
        d["LeerlingStory.Val.Achievement"] = "Belangrijk voor jou: iets goed afmaken.";
        d["LeerlingStory.Val.Stability"] = "Belangrijk voor jou: rust en duidelijkheid.";
        d["LeerlingStory.Val.Impact"] = "Belangrijk voor jou: dat anderen er iets aan hebben.";
        d["LeerlingStory.Cult.Autonomy"] = "Je voelt je thuis op een plek waar je zelf mag kiezen.";
        d["LeerlingStory.Cult.Informal"] = "Je voelt je thuis op een plek waar het informeel is.";
        d["LeerlingStory.Cult.Collaboration"] = "Je voelt je thuis op een plek waar je samen werkt.";
        d["LeerlingStory.Cult.Flexibility"] = "Je voelt je thuis op een plek waar plannen mogen veranderen.";
        d["LeerlingStory.Cult.Innovation"] = "Je voelt je thuis op een plek waar nieuwe ideeën welkom zijn.";
        d["LeerlingStory.Cult.PeopleFirst"] = "Je voelt je thuis op een plek waar mensen voorop staan.";
        d["LeerlingStory.Likes.Two"] = "Je houdt van {0} en {1}.";
        d["LeerlingStory.Likes.One"] = "Je houdt van {0}.";
        d["LeerlingStory.Tile.Bf.Samenwerken"] = "Zorgzaam en precies";
        d["LeerlingStory.Tile.Bf.Samenwerken.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Bf.Resultaatgerichtheid"] = "Afmaker";
        d["LeerlingStory.Tile.Bf.Resultaatgerichtheid.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Bf.Stressbestendigheid"] = "Rustig en sterk";
        d["LeerlingStory.Tile.Bf.Stressbestendigheid.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Bf.Innovatie"] = "Bedenker van nieuw";
        d["LeerlingStory.Tile.Bf.Innovatie.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Bf.Extraversie"] = "Graag bij mensen";
        d["LeerlingStory.Tile.Bf.Extraversie.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Bf.Balanced"] = "In balans";
        d["LeerlingStory.Tile.Bf.Balanced.Explain"] = "Zo kom jij over in de groep.";
        d["LeerlingStory.Tile.Riasec.R"] = "Maken";
        d["LeerlingStory.Tile.Riasec.R.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.I"] = "Onderzoeken";
        d["LeerlingStory.Tile.Riasec.I.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.A"] = "Creatief";
        d["LeerlingStory.Tile.Riasec.A.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.S"] = "Helpen";
        d["LeerlingStory.Tile.Riasec.S.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.E"] = "Leiden";
        d["LeerlingStory.Tile.Riasec.E.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.C"] = "Ordenen";
        d["LeerlingStory.Tile.Riasec.C.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.RS"] = "Maken en helpen";
        d["LeerlingStory.Tile.Riasec.RS.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.IR"] = "Onderzoeken en maken";
        d["LeerlingStory.Tile.Riasec.IR.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.IS"] = "Onderzoeken en helpen";
        d["LeerlingStory.Tile.Riasec.IS.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.AR"] = "Creatief en maken";
        d["LeerlingStory.Tile.Riasec.AR.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.AI"] = "Creatief en onderzoeken";
        d["LeerlingStory.Tile.Riasec.AI.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.AS"] = "Creatief en helpen";
        d["LeerlingStory.Tile.Riasec.AS.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.AE"] = "Creatief en leiden";
        d["LeerlingStory.Tile.Riasec.AE.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.AC"] = "Creatief en ordenen";
        d["LeerlingStory.Tile.Riasec.AC.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.ER"] = "Leiden en maken";
        d["LeerlingStory.Tile.Riasec.ER.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.EI"] = "Leiden en onderzoeken";
        d["LeerlingStory.Tile.Riasec.EI.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.ES"] = "Leiden en helpen";
        d["LeerlingStory.Tile.Riasec.ES.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.CR"] = "Ordenen en maken";
        d["LeerlingStory.Tile.Riasec.CR.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.CI"] = "Ordenen en onderzoeken";
        d["LeerlingStory.Tile.Riasec.CI.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.CS"] = "Ordenen en helpen";
        d["LeerlingStory.Tile.Riasec.CS.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Riasec.CE"] = "Ordenen en leiden";
        d["LeerlingStory.Tile.Riasec.CE.Explain"] = "Dit doe je graag.";
        d["LeerlingStory.Tile.Val.Autonomy"] = "Zelf kiezen";
        d["LeerlingStory.Tile.Val.Autonomy.Explain"] = "Dit vind je belangrijk.";
        d["LeerlingStory.Tile.Val.Connection"] = "Samen met anderen";
        d["LeerlingStory.Tile.Val.Connection.Explain"] = "Dit vind je belangrijk.";
        d["LeerlingStory.Tile.Val.Achievement"] = "Iets goed afmaken";
        d["LeerlingStory.Tile.Val.Achievement.Explain"] = "Dit vind je belangrijk.";
        d["LeerlingStory.Tile.Val.Stability"] = "Rust en duidelijkheid";
        d["LeerlingStory.Tile.Val.Stability.Explain"] = "Dit vind je belangrijk.";
        d["LeerlingStory.Tile.Val.Impact"] = "Verschil maken";
        d["LeerlingStory.Tile.Val.Impact.Explain"] = "Dit vind je belangrijk.";
        d["LeerlingStory.Tile.Cult.Autonomy"] = "Zelfstandig, vrij";
        d["LeerlingStory.Tile.Cult.Autonomy.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Tile.Cult.Informal"] = "Informeel en open";
        d["LeerlingStory.Tile.Cult.Informal.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Tile.Cult.Collaboration"] = "Samen in een team";
        d["LeerlingStory.Tile.Cult.Collaboration.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Tile.Cult.Flexibility"] = "Flexibel en soepel";
        d["LeerlingStory.Tile.Cult.Flexibility.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Tile.Cult.Innovation"] = "Ruimte voor ideeën";
        d["LeerlingStory.Tile.Cult.Innovation.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Tile.Cult.PeopleFirst"] = "Klein groepje, rustig";
        d["LeerlingStory.Tile.Cult.PeopleFirst.Explain"] = "Hier voel je je thuis.";
        d["LeerlingStory.Title"] = "Dit ben jij!";
        d["LeerlingStory.DonePill"] = "Klaar · {0} van {0}";
        d["LeerlingStory.Lobster"] = "Kijk, mijn nieuwe schaal glimt! En dit ben jij.";
        d["LeerlingStory.LikesTitle"] = "Je houdt van";
        d["LeerlingStory.DislikesTitle"] = "Niet zo leuk vind je";
        d["LeerlingStory.JobsTitle"] = "Beroepen om eens te bekijken";
        d["LeerlingStory.Privacy"] = "Je leraar kan dit verhaal ook zien, met jouw code. Niemand anders.";
        d["LeerlingStory.Privacy.Vo"] = "Je docent kan dit verhaal ook zien, met jouw code. Niemand anders.";
        d["LeerlingStory.Pdf"] = "PDF";
        d["LeerlingStory.CheckDream"] = "Check je droombaan";
        d["LeerlingStory.Tile.ZoBenJij"] = "Zo ben jij";
        d["LeerlingStory.Tile.DitDoeJe"] = "Dit doe je graag";
        d["LeerlingStory.Tile.Belangrijk"] = "Dit vind je belangrijk";
        d["LeerlingStory.Tile.Thuis"] = "Hier voel je je thuis";
        d["LeerlingDroom.Title"] = "Droombaan-checker";
        d["LeerlingDroom.Lead"] = "Wat wil jij later worden?";
        d["LeerlingDroom.Search"] = "Zoek een beroep";
        d["LeerlingDroom.Undecided"] = "Weet ik nog niet";
        d["LeerlingDroom.UndecidedHint"] = "Praat erover met je leraar.";
        d["LeerlingDroom.UndecidedHint.Vo"] = "Praat erover met je docent of mentor.";
        d["LeerlingDroom.HaveTitle"] = "Dit heb je al";
        d["LeerlingDroom.NeedTitle"] = "Wat heb je nodig?";
        d["LeerlingDroom.Progress"] = "{0} van 5 heb je al!";
        d["LeerlingDroom.OtherJob"] = "Andere baan";
        d["LeerlingDroom.SavePdf"] = "Bewaar als PDF";
        d["LeerlingDroom.Have.Curious"] = "Je bent nieuwsgierig — daarom kijk je hier. Dat is een goed begin.";
        d["LeerlingDroom.Cheer.Low"] = "Het is een lange reis, maar jij hebt al een goede start. Een kreeft groeit ook stap voor stap. Vraag je leraar wat je nu al kunt doen.";
        d["LeerlingDroom.Cheer.Low.Vo"] = "Het is een lange reis, maar jij hebt al een goede start. Een kreeft groeit ook stap voor stap. Vraag je docent of mentor wat je nu al kunt doen.";
        d["LeerlingDroom.Cheer.Mid"] = "Mooi! Je hebt al een paar dingen te pakken. Blijf oefenen en praat met je leraar over de volgende stap.";
        d["LeerlingDroom.Cheer.Mid.Vo"] = "Mooi! Je hebt al een paar dingen te pakken. Blijf oefenen en praat met je docent of mentor over de volgende stap.";
        d["LeerlingDroom.Cheer.High"] = "Wauw, jij past hier al goed bij. Blijf nieuwsgierig — stap voor stap kom je er.";
        d["LeerlingDroom.Lobster.Generic"] = "Welk beroep past bij jou? Laten we kijken.";
        d["LeerlingPdf.Header"] = "Mijn ontdekkingsreis";
        d["LeerlingPdf.Subheader"] = "Lobsy voor scholen · {0}";
        d["LeerlingPdf.ClassSchool"] = "Klas {0} · {1}";
        d["LeerlingPdf.NameLine"] = "Naam (vul zelf in)";
        d["LeerlingPdf.Footer"] = "Lobsy bewaart geen namen. Deze PDF is voor jou en je leraar.";
        d["LeerlingPdf.Footer.Vo"] = "Lobsy bewaart geen namen. Deze PDF is voor jou en je docent.";
        d["LeerlingPdf.DreamTitle"] = "Mijn droombaan: {0}";
        d["LeerlingPdf.Page"] = "Pagina {0} van {1}";
        d["LeerlingStory.Talk.R.Autonomy.1"] = "Vraag wanneer deze leerling het liefst iets maakt: thuis, op school of bij een club?";
        d["LeerlingStory.Talk.R.Autonomy.2"] = "Welk vak past bij 'met je handen werken'? Techniek, beeldende vorming?";
        d["LeerlingStory.Talk.R.Autonomy.3"] = "Wat zou deze leerling graag repareren of bouwen?";
        d["LeerlingStory.Talk.R.Connection.1"] = "Vraag wanneer deze leerling het liefst iets maakt: thuis, op school of bij een club?";
        d["LeerlingStory.Talk.R.Connection.2"] = "Welk vak past bij 'met je handen werken'? Techniek, beeldende vorming?";
        d["LeerlingStory.Talk.R.Connection.3"] = "Wat zou deze leerling graag repareren of bouwen?";
        d["LeerlingStory.Talk.R.Achievement.1"] = "Vraag wanneer deze leerling het liefst iets maakt: thuis, op school of bij een club?";
        d["LeerlingStory.Talk.R.Achievement.2"] = "Welk vak past bij 'met je handen werken'? Techniek, beeldende vorming?";
        d["LeerlingStory.Talk.R.Achievement.3"] = "Wat zou deze leerling graag repareren of bouwen?";
        d["LeerlingStory.Talk.R.Stability.1"] = "Vraag wanneer deze leerling het liefst iets maakt: thuis, op school of bij een club?";
        d["LeerlingStory.Talk.R.Stability.2"] = "Welk vak past bij 'met je handen werken'? Techniek, beeldende vorming?";
        d["LeerlingStory.Talk.R.Stability.3"] = "Wat zou deze leerling graag repareren of bouwen?";
        d["LeerlingStory.Talk.R.Impact.1"] = "Vraag wanneer deze leerling het liefst iets maakt: thuis, op school of bij een club?";
        d["LeerlingStory.Talk.R.Impact.2"] = "Welk vak past bij 'met je handen werken'? Techniek, beeldende vorming?";
        d["LeerlingStory.Talk.R.Impact.3"] = "Wat zou deze leerling graag repareren of bouwen?";
        d["LeerlingStory.Talk.I.Autonomy.1"] = "Vraag welk onderwerp deze leerling het liefst uitzoekt.";
        d["LeerlingStory.Talk.I.Autonomy.2"] = "Welk vak past bij 'weten hoe iets werkt'? Biologie, NaSk?";
        d["LeerlingStory.Talk.I.Autonomy.3"] = "Wanneer was deze leerling voor het laatst nieuwsgierig naar iets?";
        d["LeerlingStory.Talk.I.Connection.1"] = "Vraag welk onderwerp deze leerling het liefst uitzoekt.";
        d["LeerlingStory.Talk.I.Connection.2"] = "Welk vak past bij 'weten hoe iets werkt'? Biologie, NaSk?";
        d["LeerlingStory.Talk.I.Connection.3"] = "Wanneer was deze leerling voor het laatst nieuwsgierig naar iets?";
        d["LeerlingStory.Talk.I.Achievement.1"] = "Vraag welk onderwerp deze leerling het liefst uitzoekt.";
        d["LeerlingStory.Talk.I.Achievement.2"] = "Welk vak past bij 'weten hoe iets werkt'? Biologie, NaSk?";
        d["LeerlingStory.Talk.I.Achievement.3"] = "Wanneer was deze leerling voor het laatst nieuwsgierig naar iets?";
        d["LeerlingStory.Talk.I.Stability.1"] = "Vraag welk onderwerp deze leerling het liefst uitzoekt.";
        d["LeerlingStory.Talk.I.Stability.2"] = "Welk vak past bij 'weten hoe iets werkt'? Biologie, NaSk?";
        d["LeerlingStory.Talk.I.Stability.3"] = "Wanneer was deze leerling voor het laatst nieuwsgierig naar iets?";
        d["LeerlingStory.Talk.I.Impact.1"] = "Vraag welk onderwerp deze leerling het liefst uitzoekt.";
        d["LeerlingStory.Talk.I.Impact.2"] = "Welk vak past bij 'weten hoe iets werkt'? Biologie, NaSk?";
        d["LeerlingStory.Talk.I.Impact.3"] = "Wanneer was deze leerling voor het laatst nieuwsgierig naar iets?";
        d["LeerlingStory.Talk.A.Autonomy.1"] = "Vraag welk creatief ding deze leerling graag maakt.";
        d["LeerlingStory.Talk.A.Autonomy.2"] = "Welk vak past bij ideeën en vormgeven?";
        d["LeerlingStory.Talk.A.Autonomy.3"] = "Wanneer voelde deze leerling zich trots op iets eigens?";
        d["LeerlingStory.Talk.A.Connection.1"] = "Vraag welk creatief ding deze leerling graag maakt.";
        d["LeerlingStory.Talk.A.Connection.2"] = "Welk vak past bij ideeën en vormgeven?";
        d["LeerlingStory.Talk.A.Connection.3"] = "Wanneer voelde deze leerling zich trots op iets eigens?";
        d["LeerlingStory.Talk.A.Achievement.1"] = "Vraag welk creatief ding deze leerling graag maakt.";
        d["LeerlingStory.Talk.A.Achievement.2"] = "Welk vak past bij ideeën en vormgeven?";
        d["LeerlingStory.Talk.A.Achievement.3"] = "Wanneer voelde deze leerling zich trots op iets eigens?";
        d["LeerlingStory.Talk.A.Stability.1"] = "Vraag welk creatief ding deze leerling graag maakt.";
        d["LeerlingStory.Talk.A.Stability.2"] = "Welk vak past bij ideeën en vormgeven?";
        d["LeerlingStory.Talk.A.Stability.3"] = "Wanneer voelde deze leerling zich trots op iets eigens?";
        d["LeerlingStory.Talk.A.Impact.1"] = "Vraag welk creatief ding deze leerling graag maakt.";
        d["LeerlingStory.Talk.A.Impact.2"] = "Welk vak past bij ideeën en vormgeven?";
        d["LeerlingStory.Talk.A.Impact.3"] = "Wanneer voelde deze leerling zich trots op iets eigens?";
        d["LeerlingStory.Talk.S.Autonomy.1"] = "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?";
        d["LeerlingStory.Talk.S.Autonomy.2"] = "Welk vak past bij 'anderen helpen'? Verzorging, biologie?";
        d["LeerlingStory.Talk.S.Autonomy.3"] = "Wat zou helpen als voor de klas praten spannend voelt?";
        d["LeerlingStory.Talk.S.Connection.1"] = "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?";
        d["LeerlingStory.Talk.S.Connection.2"] = "Welk vak past bij 'anderen helpen'? Verzorging, biologie?";
        d["LeerlingStory.Talk.S.Connection.3"] = "Wat zou helpen als voor de klas praten spannend voelt?";
        d["LeerlingStory.Talk.S.Achievement.1"] = "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?";
        d["LeerlingStory.Talk.S.Achievement.2"] = "Welk vak past bij 'anderen helpen'? Verzorging, biologie?";
        d["LeerlingStory.Talk.S.Achievement.3"] = "Wat zou helpen als voor de klas praten spannend voelt?";
        d["LeerlingStory.Talk.S.Stability.1"] = "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?";
        d["LeerlingStory.Talk.S.Stability.2"] = "Welk vak past bij 'anderen helpen'? Verzorging, biologie?";
        d["LeerlingStory.Talk.S.Stability.3"] = "Wat zou helpen als voor de klas praten spannend voelt?";
        d["LeerlingStory.Talk.S.Impact.1"] = "Vraag wanneer deze leerling het liefst iemand helpt: op school, thuis of bij de sport?";
        d["LeerlingStory.Talk.S.Impact.2"] = "Welk vak past bij 'anderen helpen'? Verzorging, biologie?";
        d["LeerlingStory.Talk.S.Impact.3"] = "Wat zou helpen als voor de klas praten spannend voelt?";
        d["LeerlingStory.Talk.E.Autonomy.1"] = "Vraag wanneer deze leerling graag het voortouw neemt.";
        d["LeerlingStory.Talk.E.Autonomy.2"] = "Welk vak past bij organiseren of presenteren?";
        d["LeerlingStory.Talk.E.Autonomy.3"] = "Hoe ziet deze leerling zichzelf in een groep?";
        d["LeerlingStory.Talk.E.Connection.1"] = "Vraag wanneer deze leerling graag het voortouw neemt.";
        d["LeerlingStory.Talk.E.Connection.2"] = "Welk vak past bij organiseren of presenteren?";
        d["LeerlingStory.Talk.E.Connection.3"] = "Hoe ziet deze leerling zichzelf in een groep?";
        d["LeerlingStory.Talk.E.Achievement.1"] = "Vraag wanneer deze leerling graag het voortouw neemt.";
        d["LeerlingStory.Talk.E.Achievement.2"] = "Welk vak past bij organiseren of presenteren?";
        d["LeerlingStory.Talk.E.Achievement.3"] = "Hoe ziet deze leerling zichzelf in een groep?";
        d["LeerlingStory.Talk.E.Stability.1"] = "Vraag wanneer deze leerling graag het voortouw neemt.";
        d["LeerlingStory.Talk.E.Stability.2"] = "Welk vak past bij organiseren of presenteren?";
        d["LeerlingStory.Talk.E.Stability.3"] = "Hoe ziet deze leerling zichzelf in een groep?";
        d["LeerlingStory.Talk.E.Impact.1"] = "Vraag wanneer deze leerling graag het voortouw neemt.";
        d["LeerlingStory.Talk.E.Impact.2"] = "Welk vak past bij organiseren of presenteren?";
        d["LeerlingStory.Talk.E.Impact.3"] = "Hoe ziet deze leerling zichzelf in een groep?";
        d["LeerlingStory.Talk.C.Autonomy.1"] = "Vraag wanneer nette plannen deze leerling helpen.";
        d["LeerlingStory.Talk.C.Autonomy.2"] = "Welk vak past bij overzicht houden? Rekenen, economie?";
        d["LeerlingStory.Talk.C.Autonomy.3"] = "Wat geeft deze leerling rust in een drukke week?";
        d["LeerlingStory.Talk.C.Connection.1"] = "Vraag wanneer nette plannen deze leerling helpen.";
        d["LeerlingStory.Talk.C.Connection.2"] = "Welk vak past bij overzicht houden? Rekenen, economie?";
        d["LeerlingStory.Talk.C.Connection.3"] = "Wat geeft deze leerling rust in een drukke week?";
        d["LeerlingStory.Talk.C.Achievement.1"] = "Vraag wanneer nette plannen deze leerling helpen.";
        d["LeerlingStory.Talk.C.Achievement.2"] = "Welk vak past bij overzicht houden? Rekenen, economie?";
        d["LeerlingStory.Talk.C.Achievement.3"] = "Wat geeft deze leerling rust in een drukke week?";
        d["LeerlingStory.Talk.C.Stability.1"] = "Vraag wanneer nette plannen deze leerling helpen.";
        d["LeerlingStory.Talk.C.Stability.2"] = "Welk vak past bij overzicht houden? Rekenen, economie?";
        d["LeerlingStory.Talk.C.Stability.3"] = "Wat geeft deze leerling rust in een drukke week?";
        d["LeerlingStory.Talk.C.Impact.1"] = "Vraag wanneer nette plannen deze leerling helpen.";
        d["LeerlingStory.Talk.C.Impact.2"] = "Welk vak past bij overzicht houden? Rekenen, economie?";
        d["LeerlingStory.Talk.C.Impact.3"] = "Wat geeft deze leerling rust in een drukke week?";
        d["LeerlingStory.Class.R.1"] = "Wie in de klas maakt graag iets met de handen?";
        d["LeerlingStory.Class.R.2"] = "Welk vak voelt 'maken'?";
        d["LeerlingStory.Class.R.3"] = "Deel een klein maak-moment.";
        d["LeerlingStory.Class.I.1"] = "Wie wil graag weten hoe iets werkt?";
        d["LeerlingStory.Class.I.2"] = "Welk vak voelt 'uitzoeken'?";
        d["LeerlingStory.Class.I.3"] = "Deel een nieuwsgierige vraag.";
        d["LeerlingStory.Class.A.1"] = "Wie bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.A.2"] = "Welk vak voelt creatief?";
        d["LeerlingStory.Class.A.3"] = "Deel een idee dat je trots maakt.";
        d["LeerlingStory.Class.S.1"] = "Wie helpt graag een ander?";
        d["LeerlingStory.Class.S.2"] = "Wanneer hielp iemand in de klas?";
        d["LeerlingStory.Class.S.3"] = "Deel een moment van zorgen.";
        d["LeerlingStory.Class.E.1"] = "Wie neemt graag het voortouw?";
        d["LeerlingStory.Class.E.2"] = "Wanneer leidde iemand een groepje?";
        d["LeerlingStory.Class.E.3"] = "Deel een klein initiatief.";
        d["LeerlingStory.Class.C.1"] = "Wie houdt van nette plannen?";
        d["LeerlingStory.Class.C.2"] = "Wanneer hielp overzicht?";
        d["LeerlingStory.Class.C.3"] = "Deel een tip voor ordenen.";
        // Pair prompts name both letters. All three prompts exist for both letter orders
        // (top letter first). .2 and .3 no longer fall back to the alphabetical key.
        d["LeerlingStory.Class.AC.1"] = "Wie bedenkt graag iets nieuws en houdt van nette plannen?";
        d["LeerlingStory.Class.AC.2"] = "Welk vak voelt creatief én overzichtelijk?";
        d["LeerlingStory.Class.AC.3"] = "Deel een idee dat je netjes uitwerkte.";
        d["LeerlingStory.Class.AE.1"] = "Wie bedenkt graag iets nieuws en neemt graag het voortouw?";
        d["LeerlingStory.Class.AE.2"] = "Welk vak voelt creatief én als leiding geven?";
        d["LeerlingStory.Class.AE.3"] = "Deel een idee waarmee jij een groepje startte.";
        d["LeerlingStory.Class.AI.1"] = "Wie bedenkt graag iets nieuws en wil weten hoe het werkt?";
        d["LeerlingStory.Class.AI.2"] = "Welk vak voelt als bedenken én uitzoeken?";
        d["LeerlingStory.Class.AI.3"] = "Deel een nieuw idee en een vraag daarbij.";
        d["LeerlingStory.Class.AR.1"] = "Wie maakt graag iets moois met de handen?";
        d["LeerlingStory.Class.AR.2"] = "Welk vak voelt als maken én iets nieuws?";
        d["LeerlingStory.Class.AR.3"] = "Deel iets dat je zelf maakte.";
        d["LeerlingStory.Class.AS.1"] = "Wie bedenkt graag iets nieuws en helpt graag een ander?";
        d["LeerlingStory.Class.AS.2"] = "Welk vak voelt creatief én helpend?";
        d["LeerlingStory.Class.AS.3"] = "Deel een idee waarmee je iemand hielp.";
        d["LeerlingStory.Class.CE.1"] = "Wie neemt graag het voortouw en houdt van nette plannen?";
        d["LeerlingStory.Class.CE.2"] = "Wanneer leidde iemand met een duidelijk plan?";
        d["LeerlingStory.Class.CE.3"] = "Deel een initiatief dat overzicht gaf.";
        d["LeerlingStory.Class.CI.1"] = "Wie zoekt graag uit hoe iets werkt en houdt van nette plannen?";
        d["LeerlingStory.Class.CI.2"] = "Welk vak voelt als uitzoeken én overzicht?";
        d["LeerlingStory.Class.CI.3"] = "Deel een tip: noteer wat je uitzoekt.";
        d["LeerlingStory.Class.CR.1"] = "Wie maakt graag iets met de handen en houdt van nette plannen?";
        d["LeerlingStory.Class.CR.2"] = "Welk vak voelt als maken én overzicht?";
        d["LeerlingStory.Class.CR.3"] = "Deel een tip: eerst een plan, dan maken.";
        d["LeerlingStory.Class.CS.1"] = "Wie helpt graag een ander en houdt van nette plannen?";
        d["LeerlingStory.Class.CS.2"] = "Wanneer hielp een net plan iemand?";
        d["LeerlingStory.Class.CS.3"] = "Deel een tip om anderen netjes te helpen.";
        d["LeerlingStory.Class.EI.1"] = "Wie zoekt graag uit hoe iets werkt en neemt het voortouw?";
        d["LeerlingStory.Class.EI.2"] = "Welk vak voelt als uitzoeken én leiding geven?";
        d["LeerlingStory.Class.EI.3"] = "Deel een moment waarin jij liet uitzoeken.";
        d["LeerlingStory.Class.ER.1"] = "Wie maakt graag iets met de handen en neemt het voortouw?";
        d["LeerlingStory.Class.ER.2"] = "Welk vak voelt als maken én leiding geven?";
        d["LeerlingStory.Class.ER.3"] = "Deel een moment waarin jij het maken leidde.";
        d["LeerlingStory.Class.ES.1"] = "Wie helpt graag een ander en neemt graag het voortouw?";
        d["LeerlingStory.Class.ES.2"] = "Wanneer hielp iemand door het voortouw te nemen?";
        d["LeerlingStory.Class.ES.3"] = "Deel een moment van helpen én starten.";
        d["LeerlingStory.Class.IR.1"] = "Wie maakt graag iets met de handen en wil weten hoe het werkt?";
        d["LeerlingStory.Class.IR.2"] = "Welk vak voelt als maken én uitzoeken?";
        d["LeerlingStory.Class.IR.3"] = "Deel iets dat je maakte om het te snappen.";
        d["LeerlingStory.Class.IS.1"] = "Wie wil weten hoe iets werkt en helpt graag een ander?";
        d["LeerlingStory.Class.IS.2"] = "Welk vak voelt als uitzoeken én helpen?";
        d["LeerlingStory.Class.IS.3"] = "Deel een moment waarin uitzoeken iemand hielp.";
        d["LeerlingStory.Class.RS.1"] = "Wie helpt graag en maakt graag iets met de handen?";
        d["LeerlingStory.Class.RS.2"] = "Welk vak voelt als helpen én maken?";
        d["LeerlingStory.Class.RS.3"] = "Deel een moment waarin je hielp door te maken.";
        d["LeerlingStory.Class.CA.1"] = "Wie houdt van nette plannen en bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.EA.1"] = "Wie neemt graag het voortouw en bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.IA.1"] = "Wie wil weten hoe het werkt en bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.RA.1"] = "Wie maakt graag iets met de handen en bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.SA.1"] = "Wie helpt graag een ander en bedenkt graag iets nieuws?";
        d["LeerlingStory.Class.EC.1"] = "Wie neemt graag het voortouw en houdt van nette plannen?";
        d["LeerlingStory.Class.IC.1"] = "Wie wil weten hoe het werkt en houdt van nette plannen?";
        d["LeerlingStory.Class.RC.1"] = "Wie maakt graag iets met de handen en houdt van nette plannen?";
        d["LeerlingStory.Class.SC.1"] = "Wie helpt graag een ander en houdt van nette plannen?";
        d["LeerlingStory.Class.IE.1"] = "Wie wil weten hoe het werkt en neemt graag het voortouw?";
        d["LeerlingStory.Class.RE.1"] = "Wie maakt graag iets met de handen en neemt graag het voortouw?";
        d["LeerlingStory.Class.SE.1"] = "Wie helpt graag een ander en neemt graag het voortouw?";
        d["LeerlingStory.Class.RI.1"] = "Wie maakt graag iets met de handen en wil weten hoe het werkt?";
        d["LeerlingStory.Class.SI.1"] = "Wie helpt graag een ander en wil weten hoe het werkt?";
        d["LeerlingStory.Class.SR.1"] = "Wie helpt graag een ander en maakt graag iets met de handen?";
        d["LeerlingStory.Class.CA.2"] = "Welk vak voelt overzichtelijk én creatief?";
        d["LeerlingStory.Class.CA.3"] = "Deel een net plan met een nieuw idee.";
        d["LeerlingStory.Class.EA.2"] = "Welk vak voelt als leiding geven én creatief?";
        d["LeerlingStory.Class.EA.3"] = "Deel een moment waarin jij een groepje startte met een idee.";
        d["LeerlingStory.Class.IA.2"] = "Welk vak voelt als uitzoeken én bedenken?";
        d["LeerlingStory.Class.IA.3"] = "Deel een vraag en het nieuwe idee daarbij.";
        d["LeerlingStory.Class.RA.2"] = "Welk vak voelt als maken én bedenken?";
        d["LeerlingStory.Class.RA.3"] = "Deel iets dat je maakte en zelf bedacht.";
        d["LeerlingStory.Class.SA.2"] = "Welk vak voelt helpend én creatief?";
        d["LeerlingStory.Class.SA.3"] = "Deel hoe je iemand hielp met een idee.";
        d["LeerlingStory.Class.EC.2"] = "Welk vak voelt als leiding geven én overzichtelijk?";
        d["LeerlingStory.Class.EC.3"] = "Deel een initiatief met een duidelijk plan.";
        d["LeerlingStory.Class.IC.2"] = "Welk vak voelt als uitzoeken én een net plan?";
        d["LeerlingStory.Class.IC.3"] = "Deel wat je uitzocht en netjes noteerde.";
        d["LeerlingStory.Class.RC.2"] = "Welk vak voelt als maken én een net plan?";
        d["LeerlingStory.Class.RC.3"] = "Deel iets dat je maakte na een plan.";
        d["LeerlingStory.Class.SC.2"] = "Welk vak voelt helpend én overzichtelijk?";
        d["LeerlingStory.Class.SC.3"] = "Deel hoe een net plan iemand hielp.";
        d["LeerlingStory.Class.IE.2"] = "Welk vak voelt als uitzoeken én het voortouw?";
        d["LeerlingStory.Class.IE.3"] = "Deel een vraag waarbij jij het voortouw nam.";
        d["LeerlingStory.Class.RE.2"] = "Welk vak voelt als maken én het voortouw?";
        d["LeerlingStory.Class.RE.3"] = "Deel iets dat je maakte terwijl jij de leiding had.";
        d["LeerlingStory.Class.SE.2"] = "Welk vak voelt helpend én als leiding geven?";
        d["LeerlingStory.Class.SE.3"] = "Deel hoe je hielp door te starten.";
        d["LeerlingStory.Class.RI.2"] = "Welk vak voelt als zelf maken én uitzoeken?";
        d["LeerlingStory.Class.RI.3"] = "Deel iets dat je in elkaar zette om het te begrijpen.";
        d["LeerlingStory.Class.SI.2"] = "Welk vak voelt helpend én als uitzoeken?";
        d["LeerlingStory.Class.SI.3"] = "Deel hoe je iemand hielp door iets uit te zoeken.";
        d["LeerlingStory.Class.SR.2"] = "Welk vak voelt helpend én als maken?";
        d["LeerlingStory.Class.SR.3"] = "Deel een hulp-moment waarin je iets maakte.";
        d["LeerlingDroom.Need.ZorgVoorDieren"] = "Je bent zorgzaam, voor mensen én dieren";
        d["LeerlingDroom.Need.ZorgVoorDieren.Next"] = "Oefen met zorgen voor een dier of plant";
        d["LeerlingDroom.Need.Nieuwsgierig"] = "Je wilt weten hoe iets werkt";
        d["LeerlingDroom.Need.Nieuwsgierig.Next"] = "Kies straks vakken waarin je mag uitzoeken";
        d["LeerlingDroom.Need.Nauwkeurig"] = "Je werkt precies en netjes";
        d["LeerlingDroom.Need.Nauwkeurig.Next"] = "Oefen met netjes afronden van schoolwerk";
        d["LeerlingDroom.Need.RustigBlijven"] = "Je blijft rustig als het spannend is";
        d["LeerlingDroom.Need.RustigBlijven.Next"] = "Oefen rustig blijven als iets spannend voelt";
        d["LeerlingDroom.Need.BiologieInteresse"] = "Je houdt van dieren";
        d["LeerlingDroom.Need.BiologieInteresse.Next"] = "Kies straks biologie in je pakket";
        d["LeerlingDroom.Need.Concentratie"] = "Je kunt je goed concentreren";
        d["LeerlingDroom.Need.Concentratie.Next"] = "Oefen langere taken zonder afleiding";
        d["LeerlingDroom.Need.Techniek"] = "Je vindt techniek interessant";
        d["LeerlingDroom.Need.Techniek.Next"] = "Kies straks een techniek- of NaSk-vak";
        d["LeerlingDroom.Need.Verantwoordelijk"] = "Je neemt verantwoordelijkheid";
        d["LeerlingDroom.Need.Verantwoordelijk.Next"] = "Oefen kleine taken afmaken zonder herinnering";
        d["LeerlingDroom.Need.Ruimte"] = "Je wilt graag zelf kiezen";
        d["LeerlingDroom.Need.Ruimte.Next"] = "Praat met je leraar over zelfstandig werken";
        d["LeerlingDroom.Need.Ruimte.Next.Vo"] = "Praat met je mentor over zelfstandig werken";
        d["LeerlingDroom.Need.Rekenen"] = "Je houdt van rekenen";
        d["LeerlingDroom.Need.Rekenen.Next"] = "Oefen rekenen tot het soepel gaat";
        d["LeerlingDroom.Need.Overtuigen"] = "Je kunt anderen meekrijgen";
        d["LeerlingDroom.Need.Overtuigen.Next"] = "Oefen een kort verhaal voor de klas";
        d["LeerlingDroom.Need.Taal"] = "Je bent sterk met taal";
        d["LeerlingDroom.Need.Taal.Next"] = "Lees en schrijf regelmatig";
        d["LeerlingDroom.Need.Precies"] = "Je werkt precies";
        d["LeerlingDroom.Need.Precies.Next"] = "Oefen met foutloos overschrijven";
        d["LeerlingDroom.Need.Stress"] = "Je blijft kalm onder druk";
        d["LeerlingDroom.Need.Stress.Next"] = "Oefen ademen bij spannende momenten";
        d["LeerlingDroom.Need.Rechtvaardig"] = "Je vindt eerlijkheid belangrijk";
        d["LeerlingDroom.Need.Rechtvaardig.Next"] = "Praat over wat jij eerlijk vindt";
        d["LeerlingDroom.Need.Helpen"] = "Je helpt graag anderen";
        d["LeerlingDroom.Need.Helpen.Next"] = "Bied hulp aan bij een klasgenoot";
        d["LeerlingDroom.Need.Uitleggen"] = "Je kunt dingen uitleggen";
        d["LeerlingDroom.Need.Uitleggen.Next"] = "Oefen iets uitleggen aan een vriend";
        d["LeerlingDroom.Need.Geduld"] = "Je hebt geduld";
        d["LeerlingDroom.Need.Geduld.Next"] = "Oefen wachten en opnieuw proberen";
        d["LeerlingDroom.Need.Samen"] = "Je werkt graag samen";
        d["LeerlingDroom.Need.Samen.Next"] = "Kies vaker een groepstaak";
        d["LeerlingDroom.Need.Kinderen"] = "Je houdt van kleine kinderen";
        d["LeerlingDroom.Need.Kinderen.Next"] = "Help mee bij jongere leerlingen";
        d["LeerlingDroom.Need.Zorgen"] = "Je zorgt graag voor anderen";
        d["LeerlingDroom.Need.Zorgen.Next"] = "Oefen kleine zorg-taken thuis of op school";
        d["LeerlingDroom.Need.Uitzoeken"] = "Je wilt weten hoe iets werkt";
        d["LeerlingDroom.Need.Uitzoeken.Next"] = "Kies straks een onderzoekend vak";
        d["LeerlingDroom.Need.Rustig"] = "Je blijft rustig";
        d["LeerlingDroom.Need.Rustig.Next"] = "Oefen kalm blijven bij drukte";
        d["LeerlingDroom.Need.Impact"] = "Je wilt verschil maken";
        d["LeerlingDroom.Need.Impact.Next"] = "Kies een project dat anderen helpt";
        d["LeerlingDroom.Need.Ontwerpen"] = "Je bedenkt graag ontwerpen";
        d["LeerlingDroom.Need.Ontwerpen.Next"] = "Teken of bouw een klein ontwerp";
        d["LeerlingDroom.Need.Bouwen"] = "Je bouwt graag";
        d["LeerlingDroom.Need.Bouwen.Next"] = "Maak iets met hout, LEGO of techniek";
        d["LeerlingDroom.Need.Ideeën"] = "Je bedenkt graag nieuwe ideeën";
        d["LeerlingDroom.Need.Ideeën.Next"] = "Schrijf drie ideeën op na de les";
        d["LeerlingDroom.Need.Tekenen"] = "Je houdt van tekenen";
        d["LeerlingDroom.Need.Tekenen.Next"] = "Teken elke week iets nieuws";
        d["LeerlingDroom.Need.Maken"] = "Je maakt graag iets met je handen";
        d["LeerlingDroom.Need.Maken.Next"] = "Maak een klein project af";
        d["LeerlingDroom.Need.Creatief"] = "Je bent creatief";
        d["LeerlingDroom.Need.Creatief.Next"] = "Probeer een nieuw creatief ding";
        d["LeerlingDroom.Need.Netjes"] = "Je werkt netjes";
        d["LeerlingDroom.Need.Netjes.Next"] = "Rond werk netjes af";
        d["LeerlingDroom.Need.Drukte"] = "Je kunt tegen drukte";
        d["LeerlingDroom.Need.Drukte.Next"] = "Oefen rust houden in een drukke klas";
        d["LeerlingDroom.Need.Koken"] = "Je houdt van koken of bakken";
        d["LeerlingDroom.Need.Koken.Next"] = "Kook of bak iets eenvoudigs";
        d["LeerlingDroom.Need.Aanpakken"] = "Je pakt dingen graag aan";
        d["LeerlingDroom.Need.Aanpakken.Next"] = "Neem een kleine klus op je";
        d["LeerlingDroom.Need.Team"] = "Je werkt graag in een team";
        d["LeerlingDroom.Need.Team.Next"] = "Help je groep op gang";
        d["LeerlingDroom.Need.Kalm"] = "Je blijft kalm";
        d["LeerlingDroom.Need.Kalm.Next"] = "Oefen kalm blijven bij spanning";
        d["LeerlingDroom.Need.Sport"] = "Je houdt van sport";
        d["LeerlingDroom.Need.Sport.Next"] = "Blijf bewegen naast school";
        d["LeerlingDroom.Need.Computers"] = "Je vindt computers interessant";
        d["LeerlingDroom.Need.Computers.Next"] = "Probeer een eenvoudig digitaal project";
        d["LeerlingDroom.Need.Doorzetten"] = "Je zet door";
        d["LeerlingDroom.Need.Doorzetten.Next"] = "Maak een lastige taak af";
        d["LeerlingDroom.Need.Nieuw"] = "Je staat open voor nieuw";
        d["LeerlingDroom.Need.Nieuw.Next"] = "Probeer een nieuwe werkwijze";
        d["LeerlingDroom.Need.Gamen"] = "Je houdt van gamen";
        d["LeerlingDroom.Need.Gamen.Next"] = "Gebruik die interesse om iets te leren";
        d["LeerlingDroom.Need.Initiatief"] = "Je neemt initiatief";
        d["LeerlingDroom.Need.Initiatief.Next"] = "Start een klein idee in de klas";
        d["LeerlingDroom.Need.Mensen"] = "Je vindt mensen leuk";
        d["LeerlingDroom.Need.Mensen.Next"] = "Praat met iemand nieuws in de klas";
        d["LeerlingDroom.Need.Vrijheid"] = "Je wilt vrijheid";
        d["LeerlingDroom.Need.Vrijheid.Next"] = "Praat over zelfstandig werken";
        d["LeerlingDroom.Need.Schrijven"] = "Je schrijft graag";
        d["LeerlingDroom.Need.Schrijven.Next"] = "Schrijf een kort stukje per week";
        d["LeerlingDroom.Need.Kijken"] = "Je kijkt goed om je heen";
        d["LeerlingDroom.Need.Kijken.Next"] = "Maak foto's of teken wat je ziet";
        d["LeerlingDroom.Need.Handig"] = "Je bent handig";
        d["LeerlingDroom.Need.Handig.Next"] = "Repareer of maak iets kleins";
        d["LeerlingDroom.Need.Mode"] = "Je houdt van vormgeven";
        d["LeerlingDroom.Need.Mode.Next"] = "Teken of knip een ontwerp";
        d["LeerlingDroom.Need.Logica"] = "Je denkt logisch";
        d["LeerlingDroom.Need.Logica.Next"] = "Los puzzels of rekenopgaven op";
        d["LeerlingDroom.Need.Ordenen"] = "Je houdt van ordenen";
        d["LeerlingDroom.Need.Ordenen.Next"] = "Maak een nette planning";
        d["LeerlingDroom.Need.Probleem"] = "Je lost problemen op";
        d["LeerlingDroom.Need.Probleem.Next"] = "Zoek uit waarom iets niet werkt";
        d["LeerlingDroom.Need.Autos"] = "Je vindt techniek leuk";
        d["LeerlingDroom.Need.Autos.Next"] = "Bekijk hoe iets mechanisch werkt";
        d["LeerlingDroom.Need.Buiten"] = "Je bent graag buiten";
        d["LeerlingDroom.Need.Buiten.Next"] = "Doe iets buiten na school";
        d["LeerlingDroom.Need.Natuur"] = "Je houdt van natuur";
        d["LeerlingDroom.Need.Natuur.Next"] = "Leer een plant of dier kennen";
        d["LeerlingDroom.Need.Dieren"] = "Je houdt van dieren";
        d["LeerlingDroom.Need.Dieren.Next"] = "Help mee met dieren of natuur";
        d["LeerlingDroom.Need.Lichaam"] = "Je let op hoe het lichaam werkt";
        d["LeerlingDroom.Need.Lichaam.Next"] = "Kies straks biologie of sport";
        d["LeerlingDroom.Need.Luisteren"] = "Je luistert goed";
        d["LeerlingDroom.Need.Luisteren.Next"] = "Oefen echt luisteren naar een ander";
        d["LeerlingDroom.Need.Optreden"] = "Je durft te laten zien wat je kunt";
        d["LeerlingDroom.Need.Optreden.Next"] = "Probeer een klein optreden";
        d["LeerlingDroom.Need.Muziek"] = "Je houdt van muziek";
        d["LeerlingDroom.Need.Muziek.Next"] = "Speel of luister gericht";
        d["LeerlingDroom.Need.Durf"] = "Je durft voorop te gaan";
        d["LeerlingDroom.Need.Durf.Next"] = "Neem een kleine leidende rol";
        d["LeerlingDroom.Need.Theater"] = "Je houdt van spelen en tonen";
        d["LeerlingDroom.Need.Theater.Next"] = "Doe mee aan een toneelstukje";
        d["LeerlingDroom.Need.Presteren"] = "Je wilt graag presteren";
        d["LeerlingDroom.Need.Presteren.Next"] = "Zet een klein doel en haal het";
        d["LeerlingDroom.Need.Filmpjes"] = "Je maakt graag filmpjes";
        d["LeerlingDroom.Need.Filmpjes.Next"] = "Maak een kort filmpje over iets wat je kunt";
        d["LeerlingDroom.Need.Praten"] = "Je praat graag met mensen";
        d["LeerlingDroom.Need.Praten.Next"] = "Oefen een kort gesprek";
        d["LeerlingDroom.Need.Reizen"] = "Je houdt van onderweg zijn";
        d["LeerlingDroom.Need.Reizen.Next"] = "Praat over reizen en talen";
        d["LeerlingDroom.Need.Spelen"] = "Je speelt graag mee";
        d["LeerlingDroom.Need.Spelen.Next"] = "Help bij een spel met jongeren";
        d["LeerlingDroom.Need.Organiseren"] = "Je organiseert graag";
        d["LeerlingDroom.Need.Organiseren.Next"] = "Plan een klein groepsmoment";
        d["LeerlingDroom.Need.Betrouwbaar"] = "Je bent betrouwbaar";
        d["LeerlingDroom.Need.Betrouwbaar.Next"] = "Kom kleine afspraken na";
        d["LeerlingDroom.Need.Vertrouwen"] = "Anderen kunnen op je bouwen";
        d["LeerlingDroom.Need.Vertrouwen.Next"] = "Houd een belofte";
        d["LeerlingDroom.Need.Vroeg"] = "Je kunt vroeg starten";
        d["LeerlingDroom.Need.Vroeg.Next"] = "Oefen een vaste ochtendroutine";
        d["LeerlingDroom.Route.dierenarts.1"] = "Nu: {nu}|Kies straks biologie en scheikunde";
        d["LeerlingDroom.Route.dierenarts.2"] = "Havo of vwo afmaken|Zit je op vmbo? Via mbo Dierenartsassistent kan je ook verder.";
        d["LeerlingDroom.Route.dierenarts.3"] = "Diergeneeskunde studeren|Universiteit Utrecht · 6 jaar";
        d["LeerlingDroom.Route.dierenarts.2.Havo"] = "Vwo of hbo-propedeuse|Een havo-diploma geeft geen directe toegang. Kies vwo met biologie en scheikunde, of ga via een hbo-propedeuse.";
        d["LeerlingDroom.Route.dierenarts.3.Havo"] = "Diergeneeskunde studeren|Na vwo of na een hbo-propedeuse · 6 jaar";
        d["LeerlingDroom.Route.dierenarts.2.G78"] = "Later havo of vwo|Diergeneeskunde vraagt vwo met biologie en scheikunde, of havo en daarna een hbo-propedeuse.";
        d["LeerlingDroom.Route.dierenarts.3.G78"] = "Diergeneeskunde studeren|Pas na vwo of na een hbo-propedeuse · 6 jaar";
        d["LeerlingDroom.Route.advocaat.3.Havo"] = "Rechten studeren|Na vwo, of na havo via een hbo-propedeuse · ongeveer 4 tot 6 jaar";
        d["LeerlingDroom.Route.arts.3.Havo"] = "Geneeskunde|Na vwo met biologie, scheikunde en wiskunde · 6 jaar plus specialisatie";
        d["LeerlingDroom.Route.dierenarts.Goal"] = "Dierenarts!|In een praktijk, dierentuin of op de boerderij";
        d["LeerlingDroom.Alt.dierenarts"] = "Zit je op vmbo? Via mbo Dierenartsassistent (niveau 4) kan je ook verder.";
        d["LeerlingDroom.Route.piloot.1"] = "Nu: {nu}|Kies straks wiskunde en natuurkunde";
        d["LeerlingDroom.Route.piloot.2"] = "Havo of vwo afmaken|Goede cijfers voor bètavakken helpen";
        d["LeerlingDroom.Route.piloot.3"] = "Vliegopleiding|Vaak hbo of private school · enkele jaren";
        d["LeerlingDroom.Route.piloot.Goal"] = "Piloot!|Bij een luchtvaartmaatschappij of in de kleine luchtvaart";
        d["LeerlingDroom.Route.advocaat.1"] = "Nu: {nu}|Kies straks Nederlands en geschiedenis";
        d["LeerlingDroom.Route.advocaat.2"] = "Havo of vwo afmaken|Vwo helpt voor de universiteit";
        d["LeerlingDroom.Route.advocaat.3"] = "Rechten studeren|Universiteit · ongeveer 4 tot 6 jaar";
        d["LeerlingDroom.Route.advocaat.Goal"] = "Advocaat!|In een kantoor of bij de overheid";
        d["LeerlingDroom.Route.leraar.1"] = "Nu: {nu}|Kies vakken die je later wilt uitleggen";
        d["LeerlingDroom.Route.leraar.2"] = "Havo of vwo afmaken|Of via mbo onderwijsassistent";
        d["LeerlingDroom.Route.leraar.3"] = "Pabo of eerstegraads|Hbo pabo · ongeveer 4 jaar";
        d["LeerlingDroom.Route.leraar.Goal"] = "Leraar!|Op een basisschool of middelbare school";
        d["LeerlingDroom.Alt.leraar"] = "Via mbo Onderwijsassistent kun je ook verder richting onderwijs.";
        d["LeerlingDroom.Route.arts.1"] = "Nu: {nu}|Kies straks biologie, scheikunde en wiskunde";
        d["LeerlingDroom.Route.arts.2"] = "Vwo afmaken|Geneeskunde vraagt vaak vwo";
        d["LeerlingDroom.Route.arts.3"] = "Geneeskunde|Universiteit · 6 jaar plus specialisatie";
        d["LeerlingDroom.Route.arts.Goal"] = "Arts!|In een ziekenhuis of huisartspraktijk";
        d["LeerlingDroom.Route.architect.1"] = "Nu: {nu}|Kies straks tekenen, wiskunde en techniek";
        d["LeerlingDroom.Route.architect.2"] = "Havo of vwo afmaken|Technische route helpt";
        d["LeerlingDroom.Route.architect.3"] = "Bouwkunde of Architectuur|Hbo of wo · 4 tot 5 jaar";
        d["LeerlingDroom.Route.architect.Goal"] = "Architect!|Bij een bureau of in de bouw";
        d["LeerlingDroom.Route.kok.1"] = "Nu: {nu}|Kies straks consumptief of economie";
        d["LeerlingDroom.Route.kok.2"] = "Vmbo of havo|Praktijkervaring telt mee";
        d["LeerlingDroom.Route.kok.3"] = "Mbo Kok|Mbo niveau 2 tot 4 · 2 tot 4 jaar";
        d["LeerlingDroom.Route.kok.Goal"] = "Kok!|In een restaurant, keuken of catering";
        d["LeerlingDroom.Alt.kok"] = "Via mbo Kok of Gastheer/-vrouw kun je snel aan de slag.";
        d["LeerlingDroom.Route.brandweer.1"] = "Nu: {nu}|Blijf sporten en samenwerken oefenen";
        d["LeerlingDroom.Route.brandweer.2"] = "Diploma middelbaar|Vmbo, havo of vwo";
        d["LeerlingDroom.Route.brandweer.3"] = "Brandweeropleiding|Interne opleiding bij de brandweer";
        d["LeerlingDroom.Route.brandweer.Goal"] = "Brandweer!|Bij een kazerne in je regio";
        d["LeerlingDroom.Route.game-developer.1"] = "Nu: {nu}|Kies straks informatica of wiskunde";
        d["LeerlingDroom.Route.game-developer.2"] = "Havo of vwo|Of mbo software development";
        d["LeerlingDroom.Route.game-developer.3"] = "Game of software-opleiding|Mbo of hbo · 3 tot 4 jaar";
        d["LeerlingDroom.Route.game-developer.Goal"] = "Game developer!|Bij een studio of als zelfstandige";
        d["LeerlingDroom.Alt.game-developer"] = "Via mbo Software developer kun je ook verder.";
        d["LeerlingDroom.Route.astronaut.1"] = "Nu: {nu}|Kies straks wiskunde, natuurkunde en Engels";
        d["LeerlingDroom.Route.astronaut.2"] = "Vwo afmaken|Sterke bèta-cijfers helpen";
        d["LeerlingDroom.Route.astronaut.3"] = "Technische of vliegopleiding|Lange route via wo of luchtvaart";
        d["LeerlingDroom.Route.astronaut.Goal"] = "Astronaut!|Bij een ruimtevaartorganisatie — een zeldzaam doel";
        d["LeerlingDroom.Route.politie.1"] = "Nu: {nu}|Oefen samenwerken en rustig blijven";
        d["LeerlingDroom.Route.politie.2"] = "Diploma middelbaar|Vmbo-kader of hoger";
        d["LeerlingDroom.Route.politie.3"] = "Politieopleiding|Politieacademie · enkele jaren";
        d["LeerlingDroom.Route.politie.Goal"] = "Politieagent!|Op straat, op kantoor of in een team";
        d["LeerlingDroom.Route.verpleegkundige.1"] = "Nu: {nu}|Kies straks biologie en zorg & welzijn";
        d["LeerlingDroom.Route.verpleegkundige.2"] = "Vmbo, havo of vwo|Zorgprofiel helpt";
        d["LeerlingDroom.Route.verpleegkundige.3"] = "Mbo of hbo Verpleegkunde|3 tot 4 jaar · daarna BIG-registratie";
        d["LeerlingDroom.Route.verpleegkundige.Goal"] = "Verpleegkundige!|In een ziekenhuis, wijk of zorginstelling";
        d["LeerlingDroom.Alt.verpleegkundige"] = "Via mbo Verpleegkunde kun je ook verder.";
        d["LeerlingDroom.Route.ondernemer.1"] = "Nu: {nu}|Oefen een klein idee uitwerken";
        d["LeerlingDroom.Route.ondernemer.2"] = "Diploma middelbaar|Elk niveau kan";
        d["LeerlingDroom.Route.ondernemer.3"] = "Mbo of hbo bedrijfskunde|Of starten naast school";
        d["LeerlingDroom.Route.ondernemer.Goal"] = "Ondernemer!|Met een eigen zaak of project";
        d["LeerlingDroom.Route.journalist.1"] = "Nu: {nu}|Kies straks Nederlands en geschiedenis";
        d["LeerlingDroom.Route.journalist.2"] = "Havo of vwo|Schrijven oefenen helpt";
        d["LeerlingDroom.Route.journalist.3"] = "Journalistiek|Hbo · ongeveer 4 jaar";
        d["LeerlingDroom.Route.journalist.Goal"] = "Journalist!|Bij een medium of als freelancer";
        d["LeerlingDroom.Route.fotograaf.1"] = "Nu: {nu}|Oefen foto's maken en kijken";
        d["LeerlingDroom.Route.fotograaf.2"] = "Vmbo, havo of vwo|Portfolio telt";
        d["LeerlingDroom.Route.fotograaf.3"] = "Mbo Fotografie|Of hbo beeldende opleiding · 3 tot 4 jaar";
        d["LeerlingDroom.Route.fotograaf.Goal"] = "Fotograaf!|In een studio, op locatie of freelance";
        d["LeerlingDroom.Alt.fotograaf"] = "Via mbo Mediavormgever of Fotografie kun je starten.";
        d["LeerlingDroom.Route.kapper.1"] = "Nu: {nu}|Oefen netjes werken met je handen";
        d["LeerlingDroom.Route.kapper.2"] = "Vmbo of mavo|Praktijkroute past goed";
        d["LeerlingDroom.Route.kapper.3"] = "Mbo Kapper|Niveau 2 tot 3 · 2 tot 3 jaar";
        d["LeerlingDroom.Route.kapper.Goal"] = "Kapper!|In een salon of als zelfstandige";
        d["LeerlingDroom.Alt.kapper"] = "Via mbo Kapper kun je snel aan de slag.";
        d["LeerlingDroom.Route.programmeur.1"] = "Nu: {nu}|Kies straks informatica of wiskunde";
        d["LeerlingDroom.Route.programmeur.2"] = "Havo of vwo|Of mbo software";
        d["LeerlingDroom.Route.programmeur.3"] = "Mbo of hbo ICT|3 tot 4 jaar";
        d["LeerlingDroom.Route.programmeur.Goal"] = "Programmeur!|Bij een bedrijf of als freelancer";
        d["LeerlingDroom.Alt.programmeur"] = "Via mbo Software developer kun je ook verder.";
        d["LeerlingDroom.Route.bouwkundige.1"] = "Nu: {nu}|Kies straks wiskunde en techniek";
        d["LeerlingDroom.Route.bouwkundige.2"] = "Havo of vwo|Of mbo bouwkunde";
        d["LeerlingDroom.Route.bouwkundige.3"] = "Bouwkunde|Mbo of hbo · 3 tot 4 jaar";
        d["LeerlingDroom.Route.bouwkundige.Goal"] = "Bouwkundige!|Op kantoor of op de bouwplaats";
        d["LeerlingDroom.Alt.bouwkundige"] = "Via mbo Bouwkunde kun je ook verder.";
        d["LeerlingDroom.Route.tandarts.1"] = "Nu: {nu}|Kies straks biologie en scheikunde";
        d["LeerlingDroom.Route.tandarts.2"] = "Vwo afmaken|Tandheelkunde vraagt vaak vwo";
        d["LeerlingDroom.Route.tandarts.3"] = "Tandheelkunde|Universiteit · 6 jaar";
        d["LeerlingDroom.Route.tandarts.Goal"] = "Tandarts!|In een praktijk of kliniek";
        d["LeerlingDroom.Route.psycholoog.1"] = "Nu: {nu}|Kies straks biologie en maatschappijleer";
        d["LeerlingDroom.Route.psycholoog.2"] = "Vwo afmaken|Universiteit helpt";
        d["LeerlingDroom.Route.psycholoog.3"] = "Psychologie|Universiteit · 3 plus 1 of 2 jaar";
        d["LeerlingDroom.Route.psycholoog.Goal"] = "Psycholoog!|In de zorg, op school of in een praktijk";
        d["LeerlingDroom.Route.fysiotherapeut.1"] = "Nu: {nu}|Kies straks biologie en sport";
        d["LeerlingDroom.Route.fysiotherapeut.2"] = "Havo of vwo|Hbo-route";
        d["LeerlingDroom.Route.fysiotherapeut.3"] = "Fysiotherapie|Hbo · 4 jaar";
        d["LeerlingDroom.Route.fysiotherapeut.Goal"] = "Fysiotherapeut!|In een praktijk of ziekenhuis";
        d["LeerlingDroom.Alt.fysiotherapeut"] = "Via mbo Sport en bewegen kun je oriënteren.";
        d["LeerlingDroom.Route.verloskundige.1"] = "Nu: {nu}|Kies straks biologie en zorg";
        d["LeerlingDroom.Route.verloskundige.2"] = "Havo of vwo|Hbo-route";
        d["LeerlingDroom.Route.verloskundige.3"] = "Verloskunde|Hbo · 4 jaar";
        d["LeerlingDroom.Route.verloskundige.Goal"] = "Verloskundige!|In een praktijk of ziekenhuis";
        d["LeerlingDroom.Route.apotheker.1"] = "Nu: {nu}|Kies straks scheikunde en biologie";
        d["LeerlingDroom.Route.apotheker.2"] = "Vwo afmaken|Universiteit";
        d["LeerlingDroom.Route.apotheker.3"] = "Farmacie|Universiteit · 6 jaar";
        d["LeerlingDroom.Route.apotheker.Goal"] = "Apotheker!|In een apotheek of ziekenhuis";
        d["LeerlingDroom.Route.rechter.1"] = "Nu: {nu}|Kies straks Nederlands en geschiedenis";
        d["LeerlingDroom.Route.rechter.2"] = "Vwo afmaken|Rechtenstudie";
        d["LeerlingDroom.Route.rechter.3"] = "Rechten plus ervaring|Lange route via wo en praktijk";
        d["LeerlingDroom.Route.rechter.Goal"] = "Rechter!|Bij de rechtspraak";
        d["LeerlingDroom.Route.notaris.1"] = "Nu: {nu}|Kies straks Nederlands en economie";
        d["LeerlingDroom.Route.notaris.2"] = "Vwo afmaken|Notarieel recht";
        d["LeerlingDroom.Route.notaris.3"] = "Notarieel recht|Universiteit · daarna stage";
        d["LeerlingDroom.Route.notaris.Goal"] = "Notaris!|In een notariskantoor";
        d["LeerlingDroom.Route.ingenieur.1"] = "Nu: {nu}|Kies straks wiskunde, natuurkunde en techniek";
        d["LeerlingDroom.Route.ingenieur.2"] = "Havo of vwo|Technische route";
        d["LeerlingDroom.Route.ingenieur.3"] = "Technische opleiding|Hbo of wo · 4 tot 5 jaar";
        d["LeerlingDroom.Route.ingenieur.Goal"] = "Ingenieur!|In de techniek, bouw of industrie";
        d["LeerlingDroom.Alt.ingenieur"] = "Via mbo Techniek kun je doorstromen naar hbo.";
        d["LeerlingDroom.Route.elektricien.1"] = "Nu: {nu}|Kies straks techniek of NaSk";
        d["LeerlingDroom.Route.elektricien.2"] = "Vmbo of havo|Praktijkroute";
        d["LeerlingDroom.Route.elektricien.3"] = "Mbo Elektricien|Niveau 2 tot 4 · 2 tot 4 jaar";
        d["LeerlingDroom.Route.elektricien.Goal"] = "Elektricien!|Bij installatiebedrijven of in de bouw";
        d["LeerlingDroom.Alt.elektricien"] = "Via mbo Elektrotechniek kun je snel starten.";
        d["LeerlingDroom.Route.automonteur.1"] = "Nu: {nu}|Kies straks techniek";
        d["LeerlingDroom.Route.automonteur.2"] = "Vmbo|Praktijkroute past goed";
        d["LeerlingDroom.Route.automonteur.3"] = "Mbo Autotechniek|Niveau 2 tot 4 · 2 tot 4 jaar";
        d["LeerlingDroom.Route.automonteur.Goal"] = "Automonteur!|In een garage of werkplaats";
        d["LeerlingDroom.Alt.automonteur"] = "Via mbo Autotechniek kun je snel starten.";
        d["LeerlingDroom.Route.timmerman.1"] = "Nu: {nu}|Kies straks techniek of bouwen";
        d["LeerlingDroom.Route.timmerman.2"] = "Vmbo|Praktijkroute";
        d["LeerlingDroom.Route.timmerman.3"] = "Mbo Timmerman|Niveau 2 tot 3 · 2 tot 3 jaar";
        d["LeerlingDroom.Route.timmerman.Goal"] = "Timmerman!|Op de bouw of in een werkplaats";
        d["LeerlingDroom.Alt.timmerman"] = "Via mbo Timmeren kun je snel starten.";
        d["LeerlingDroom.Route.hovenier.1"] = "Nu: {nu}|Kies straks biologie of groen";
        d["LeerlingDroom.Route.hovenier.2"] = "Vmbo|Buiten werken past";
        d["LeerlingDroom.Route.hovenier.3"] = "Mbo Hovenier|Niveau 2 tot 3 · 2 tot 3 jaar";
        d["LeerlingDroom.Route.hovenier.Goal"] = "Hovenier!|In tuinen, parken of groenvoorziening";
        d["LeerlingDroom.Alt.hovenier"] = "Via mbo Hovenier kun je snel starten.";
        d["LeerlingDroom.Route.bioloog.1"] = "Nu: {nu}|Kies straks biologie en scheikunde";
        d["LeerlingDroom.Route.bioloog.2"] = "Havo of vwo|Universiteit of hbo";
        d["LeerlingDroom.Route.bioloog.3"] = "Biologie|Wo of hbo · 3 tot 5 jaar";
        d["LeerlingDroom.Route.bioloog.Goal"] = "Bioloog!|In onderzoek, natuur of onderwijs";
        d["LeerlingDroom.Route.wetenschapper.1"] = "Nu: {nu}|Kies straks bètavakken";
        d["LeerlingDroom.Route.wetenschapper.2"] = "Vwo afmaken|Onderzoek vraagt vaak wo";
        d["LeerlingDroom.Route.wetenschapper.3"] = "Wetenschappelijke studie|Universiteit · 3 plus master";
        d["LeerlingDroom.Route.wetenschapper.Goal"] = "Wetenschapper!|In een lab, universiteit of instituut";
        d["LeerlingDroom.Route.grafisch-ontwerper.1"] = "Nu: {nu}|Kies straks tekenen of digitaal";
        d["LeerlingDroom.Route.grafisch-ontwerper.2"] = "Vmbo, havo of vwo|Portfolio telt";
        d["LeerlingDroom.Route.grafisch-ontwerper.3"] = "Mbo of hbo vormgeving|3 tot 4 jaar";
        d["LeerlingDroom.Route.grafisch-ontwerper.Goal"] = "Grafisch ontwerper!|Bij een bureau of als freelancer";
        d["LeerlingDroom.Alt.grafisch-ontwerper"] = "Via mbo Mediavormgever kun je starten.";
        d["LeerlingDroom.Route.muzikant.1"] = "Nu: {nu}|Blijf oefenen op je instrument";
        d["LeerlingDroom.Route.muzikant.2"] = "Diploma middelbaar|Conservatorium of mbo muziek";
        d["LeerlingDroom.Route.muzikant.3"] = "Muziekopleiding|Mbo of hbo · enkele jaren";
        d["LeerlingDroom.Route.muzikant.Goal"] = "Muzikant!|Op podium, in les of studio";
        d["LeerlingDroom.Alt.muzikant"] = "Via muziekscholen en mbo Muziek kun je groeien.";
        d["LeerlingDroom.Route.acteur.1"] = "Nu: {nu}|Doe mee aan toneel of presenteren";
        d["LeerlingDroom.Route.acteur.2"] = "Diploma middelbaar|Toneelschool of mbo";
        d["LeerlingDroom.Route.acteur.3"] = "Toneelopleiding|Hbo of mbo · enkele jaren";
        d["LeerlingDroom.Route.acteur.Goal"] = "Acteur!|Op toneel, film of televisie";
        d["LeerlingDroom.Alt.acteur"] = "Via mbo Acteren kun je oriënteren.";
        d["LeerlingDroom.Route.profsporter.1"] = "Nu: {nu}|Train gericht naast school";
        d["LeerlingDroom.Route.profsporter.2"] = "Diploma middelbaar|Topsportrajecten bestaan";
        d["LeerlingDroom.Route.profsporter.3"] = "Talentontwikkeling|Club, bond en soms sportopleiding";
        d["LeerlingDroom.Route.profsporter.Goal"] = "Profsporter!|Bij een club of bond — een zeldzaam pad";
        d["LeerlingDroom.Route.personal-trainer.1"] = "Nu: {nu}|Blijf sporten en mensen helpen";
        d["LeerlingDroom.Route.personal-trainer.2"] = "Vmbo, havo of vwo|Sportopleiding helpt";
        d["LeerlingDroom.Route.personal-trainer.3"] = "Mbo Sport of fitness|2 tot 3 jaar plus certificaten";
        d["LeerlingDroom.Route.personal-trainer.Goal"] = "Personal trainer!|In een sportschool of als coach";
        d["LeerlingDroom.Alt.personal-trainer"] = "Via mbo Sport en bewegen kun je starten.";
        d["LeerlingDroom.Route.contentmaker.1"] = "Nu: {nu}|Oefen filmpjes en verhalen maken";
        d["LeerlingDroom.Route.contentmaker.2"] = "Vmbo, havo of vwo|Portfolio telt";
        d["LeerlingDroom.Route.contentmaker.3"] = "Mbo media of creatief|2 tot 4 jaar";
        d["LeerlingDroom.Route.contentmaker.Goal"] = "Contentmaker!|Online, bij media of als freelancer";
        d["LeerlingDroom.Alt.contentmaker"] = "Via mbo Mediavormgever kun je starten.";
        d["LeerlingDroom.Route.marketeer.1"] = "Nu: {nu}|Kies straks economie of Nederlands";
        d["LeerlingDroom.Route.marketeer.2"] = "Havo of vwo|Of mbo marketing";
        d["LeerlingDroom.Route.marketeer.3"] = "Marketing opleiding|Mbo of hbo · 3 tot 4 jaar";
        d["LeerlingDroom.Route.marketeer.Goal"] = "Marketeer!|Bij een bedrijf of bureau";
        d["LeerlingDroom.Route.accountant.1"] = "Nu: {nu}|Kies straks economie en wiskunde";
        d["LeerlingDroom.Route.accountant.2"] = "Havo of vwo|Of mbo finance";
        d["LeerlingDroom.Route.accountant.3"] = "Accountancy|Mbo of hbo · 3 tot 4 jaar";
        d["LeerlingDroom.Route.accountant.Goal"] = "Accountant!|Bij een kantoor of bedrijf";
        d["LeerlingDroom.Alt.accountant"] = "Via mbo Business Services kun je starten.";
        d["LeerlingDroom.Route.makelaar.1"] = "Nu: {nu}|Oefen praten en plannen";
        d["LeerlingDroom.Route.makelaar.2"] = "Havo of vwo|Of mbo vastgoed";
        d["LeerlingDroom.Route.makelaar.3"] = "Vastgoedopleiding|Mbo of hbo · plus branchecertificaat";
        d["LeerlingDroom.Route.makelaar.Goal"] = "Makelaar!|In de woning- of bedrijfsmarkt";
        d["LeerlingDroom.Route.cabinepersoneel.1"] = "Nu: {nu}|Oefen talen en klantcontact";
        d["LeerlingDroom.Route.cabinepersoneel.2"] = "Diploma middelbaar|Havo helpt vaak";
        d["LeerlingDroom.Route.cabinepersoneel.3"] = "Luchtvaartopleiding|Korte opleiding bij een airline";
        d["LeerlingDroom.Route.cabinepersoneel.Goal"] = "Cabinepersoneel!|In een vliegtuigteam";
        d["LeerlingDroom.Route.militair.1"] = "Nu: {nu}|Blijf sporten en samenwerken";
        d["LeerlingDroom.Route.militair.2"] = "Diploma middelbaar|Afhankelijk van functie";
        d["LeerlingDroom.Route.militair.3"] = "Defensieopleiding|Interne opleiding bij Defensie";
        d["LeerlingDroom.Route.militair.Goal"] = "Militair!|Bij Defensie in verschillende functies";
        d["LeerlingDroom.Route.pedagogisch-medewerker.1"] = "Nu: {nu}|Kies straks zorg & welzijn";
        d["LeerlingDroom.Route.pedagogisch-medewerker.2"] = "Vmbo of havo|Praktijkroute past";
        d["LeerlingDroom.Route.pedagogisch-medewerker.3"] = "Mbo Pedagogisch werk|Niveau 3 tot 4 · 3 tot 4 jaar";
        d["LeerlingDroom.Route.pedagogisch-medewerker.Goal"] = "Pedagogisch medewerker!|In de kinderopvang of bso";
        d["LeerlingDroom.Alt.pedagogisch-medewerker"] = "Via mbo Pedagogisch medewerker kun je starten.";
        d["LeerlingDroom.Route.dierenverzorger.1"] = "Nu: {nu}|Kies straks biologie of groen";
        d["LeerlingDroom.Route.dierenverzorger.2"] = "Vmbo|Praktijkroute";
        d["LeerlingDroom.Route.dierenverzorger.3"] = "Mbo Dier|Niveau 2 tot 4 · 2 tot 4 jaar";
        d["LeerlingDroom.Route.dierenverzorger.Goal"] = "Dierenverzorger!|In een asiel, pension of dierentuin";
        d["LeerlingDroom.Alt.dierenverzorger"] = "Via mbo Dier kun je starten.";
        d["LeerlingDroom.Route.evenementenorganisator.1"] = "Nu: {nu}|Oefen organiseren van een klein event";
        d["LeerlingDroom.Route.evenementenorganisator.2"] = "Havo of vwo|Of mbo events";
        d["LeerlingDroom.Route.evenementenorganisator.3"] = "Event of leisure opleiding|Mbo of hbo · 3 tot 4 jaar";
        d["LeerlingDroom.Route.evenementenorganisator.Goal"] = "Evenementenorganisator!|Bij festivals, bedrijven of bureaus";
        d["LeerlingDroom.Alt.evenementenorganisator"] = "Via mbo Leisure kun je starten.";
        d["LeerlingDroom.Route.data-scientist.1"] = "Nu: {nu}|Kies straks wiskunde en informatica";
        d["LeerlingDroom.Route.data-scientist.2"] = "Havo of vwo|Sterke bèta helpt";
        d["LeerlingDroom.Route.data-scientist.3"] = "Data of AI opleiding|Hbo of wo · 3 tot 5 jaar";
        d["LeerlingDroom.Route.data-scientist.Goal"] = "Data scientist!|Bij bedrijven of onderzoek";
        d["LeerlingDroom.Route.bakker.1"] = "Nu: {nu}|Oefen vroeg opstaan en netjes werken";
        d["LeerlingDroom.Route.bakker.2"] = "Vmbo|Praktijkroute";
        d["LeerlingDroom.Route.bakker.3"] = "Mbo Bakker|Niveau 2 tot 3 · 2 tot 3 jaar";
        d["LeerlingDroom.Route.bakker.Goal"] = "Bakker!|In een bakkerij of keuken";
        d["LeerlingDroom.Alt.bakker"] = "Via mbo Bakker kun je snel starten.";
        d["LeerlingDroom.Route.logistiek-medewerker.1"] = "Nu: {nu}|Oefen ordenen en samenwerken";
        d["LeerlingDroom.Route.logistiek-medewerker.2"] = "Mbo logistiek (niveau 2–4)|Vaak 2 tot 4 jaar, ook via bbl";
        d["LeerlingDroom.Route.logistiek-medewerker.3"] = "Stage / bbl|Oefen in een magazijn of op school";
        d["LeerlingDroom.Route.logistiek-medewerker.Goal"] = "Logistiek medewerker!|In een magazijn, haven of transport";
        d["LeerlingDroom.Alt.logistiek-medewerker"] = "Via mbo logistiek kun je snel starten.";
        d["LeerlingDroom.Route.verkoper.1"] = "Nu: {nu}|Oefen praten en helpen";
        d["LeerlingDroom.Route.verkoper.2"] = "Mbo verkoop (niveau 2–4)|Vaak 2 tot 4 jaar, ook via bbl";
        d["LeerlingDroom.Route.verkoper.3"] = "Stage / bbl|Oefen in een winkel of op school";
        d["LeerlingDroom.Route.verkoper.Goal"] = "Verkoper!|In een winkel of webshop";
        d["LeerlingDroom.Alt.verkoper"] = "Via mbo verkoop kun je snel starten.";
        d["LeerlingDroom.Route.beveiliger.1"] = "Nu: {nu}|Oefen kalm blijven en samenwerken";
        d["LeerlingDroom.Route.beveiliger.2"] = "Mbo beveiliging (niveau 2–3)|Vaak 2 tot 3 jaar, ook via bbl";
        d["LeerlingDroom.Route.beveiliger.3"] = "Stage / bbl|Oefen alert zijn in een team";
        d["LeerlingDroom.Route.beveiliger.Goal"] = "Beveiliger!|Op school, evenement of terrein";
        d["LeerlingDroom.Alt.beveiliger"] = "Via mbo beveiliging kun je snel starten.";
        d["LeerlingDroom.Route.installateur.1"] = "Nu: {nu}|Kies straks techniek of NaSk";
        d["LeerlingDroom.Route.installateur.2"] = "Mbo installatie (niveau 2–4)|Vaak 2 tot 4 jaar, ook via bbl";
        d["LeerlingDroom.Route.installateur.3"] = "Stage / bbl|Oefen leidingen of panelen maken";
        d["LeerlingDroom.Route.installateur.Goal"] = "Installateur!|In huizen, scholen of gebouwen";
        d["LeerlingDroom.Alt.installateur"] = "Via mbo installatie kun je snel starten.";
        d["LeerlingDroom.Route.verzorgende-ig.1"] = "Nu: {nu}|Kies straks zorg & welzijn";
        d["LeerlingDroom.Route.verzorgende-ig.2"] = "Mbo verzorgende IG (niveau 3)|Vaak 3 jaar, ook via bbl";
        d["LeerlingDroom.Route.verzorgende-ig.3"] = "Stage / bbl|Oefen zorgen in een team";
        d["LeerlingDroom.Route.verzorgende-ig.Goal"] = "Verzorgende IG!|In de zorg of thuiszorg";
        d["LeerlingDroom.Alt.verzorgende-ig"] = "Via mbo zorg kun je starten.";
        d["LeerlingDroom.Route.schilder.1"] = "Nu: {nu}|Oefen netjes werken met je handen";
        d["LeerlingDroom.Route.schilder.2"] = "Mbo schilderen (niveau 2–3)|Vaak 2 tot 3 jaar, ook via bbl";
        d["LeerlingDroom.Route.schilder.3"] = "Stage / bbl|Oefen verven en afwerken";
        d["LeerlingDroom.Route.schilder.Goal"] = "Schilder!|In huizen, scholen of buiten";
        d["LeerlingDroom.Alt.schilder"] = "Via mbo schilderen kun je snel starten.";
        d["LeerlingDroom.Route.loodgieter.1"] = "Nu: {nu}|Kies straks techniek";
        d["LeerlingDroom.Route.loodgieter.2"] = "Mbo sanitair (niveau 2–3)|Vaak 2 tot 3 jaar, ook via bbl";
        d["LeerlingDroom.Route.loodgieter.3"] = "Stage / bbl|Oefen leidingen maken en herstellen";
        d["LeerlingDroom.Route.loodgieter.Goal"] = "Loodgieter!|In huizen, scholen of gebouwen";
        d["LeerlingDroom.Alt.loodgieter"] = "Via mbo sanitair kun je snel starten.";
        d["LeerlingDroom.Route.doktersassistent.1"] = "Nu: {nu}|Kies straks biologie en zorg";
        d["LeerlingDroom.Route.doktersassistent.2"] = "Mbo doktersassistent (niveau 4)|Vaak 3 tot 4 jaar";
        d["LeerlingDroom.Route.doktersassistent.3"] = "Stage / bbl|Oefen helpen in een praktijk";
        d["LeerlingDroom.Route.doktersassistent.Goal"] = "Doktersassistent!|In een huisartspraktijk of ziekenhuis";
        d["LeerlingDroom.Alt.doktersassistent"] = "Via mbo doktersassistent kun je starten.";
        d["LeerlingDroom.Route.ict-medewerker.1"] = "Nu: {nu}|Kies straks informatica of wiskunde";
        d["LeerlingDroom.Route.ict-medewerker.2"] = "Mbo ICT (niveau 3–4)|Vaak 3 tot 4 jaar, ook via bbl";
        d["LeerlingDroom.Route.ict-medewerker.3"] = "Stage / bbl|Oefen computers helpen op school";
        d["LeerlingDroom.Route.ict-medewerker.Goal"] = "ICT-medewerker!|Op school of bij een helpdesk";
        d["LeerlingDroom.Alt.ict-medewerker"] = "Via mbo ICT kun je snel starten.";
        d["LeerlingDroom.Route.horecamedewerker.1"] = "Nu: {nu}|Oefen koken of mensen helpen";
        d["LeerlingDroom.Route.horecamedewerker.2"] = "Mbo horeca (niveau 2–4)|Vaak 2 tot 4 jaar, ook via bbl";
        d["LeerlingDroom.Route.horecamedewerker.3"] = "Stage / bbl|Oefen in een keuken of restaurant";
        d["LeerlingDroom.Route.horecamedewerker.Goal"] = "Horecamedewerker!|In een keuken, restaurant of kantine";
        d["LeerlingDroom.Alt.horecamedewerker"] = "Via mbo horeca kun je snel starten.";
        d["LeerlingStory.Section.Story"] = "Dit ben jij";
        return d;
    }
}
