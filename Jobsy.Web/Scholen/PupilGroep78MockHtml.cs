using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Scholen;

internal static class PupilGroep78MockHtml
{
    internal static readonly (string Key, string TitleNl, string Domain, string Blurb)[] FallbackJobPicks =
    [
        ("dierenverzorger", "Dierenverzorger", "Dieren en leven", "Je zorgt voor dieren: eten geven, verzorgen en kijken of ze gezond zijn."),
        ("bioloog", "Bioloog", "Uitzoeken en wetenschap", "Je ontdekt hoe planten en dieren leven. Soms buiten, soms in een lab."),
        ("grafisch-ontwerper", "Grafisch ontwerper", "Creatief en ontwerpen", "Je bedenkt en maakt plaatjes, logo's en andere creatieve dingen."),
        ("hovenier", "Hovenier", "Natuur en buiten", "Je maakt tuinen en parken mooi. Je bent bijna de hele dag buiten."),
        ("brandweer", "Brandweerman/-vrouw", "Veiligheid en hulp", "Je helpt mensen als er brand is of als iemand in de knel zit."),
    ];

    internal static string PchipButton(
        CultureState culture,
        PupilInterestChipCatalog.Chip chip,
        bool on,
        bool dislike)
    {
        var cls = on ? $"pchip on {(dislike ? "dislike" : "like")}" : "pchip";
        var attr = dislike ? "data-ll-chip-dislike" : "data-ll-chip-like";
        return $"""
            <button type="button" class="{cls}" {attr}="{PupilLeerlingSsrParts.E(chip.Key)}"><span class="ico">{PupilGroep78ChipIcons.Svg(chip.Key)}</span>{PupilLeerlingSsrParts.E(culture[chip.LabelKey])}</button>
            """;
    }

    internal static string PuzzlePreviewRow(bool ariaHidden = true)
    {
        var sb = new StringBuilder("""<div class="prev-row-box"><div class="prev-row" """);
        sb.Append(ariaHidden ? "aria-hidden=\"true\"" : "");
        sb.Append('>');
        foreach (var n in PupilJourneyTheme.SchelpenrijPatternSequence())
        {
            sb.Append("<div class=\"slot\"><img src=\"/leerling/werelden/img/schelp-").Append(n).Append(".png\" alt=\"\" /></div>");
        }

        sb.Append("""<div class="slot q" data-ll-puzzle-slot-q="1">?</div></div></div>""");
        return sb.ToString();
    }

    internal static string PuzzleMatchPreviewRow(string? themeKey)
    {
        var sb = new StringBuilder("""<div class="prev-row-box prev-row-box--cards"><div class="prev-row prev-row--cards" aria-hidden="true">""");
        foreach (var face in PupilJourneyTheme.PuzzleMatchPairIds(themeKey))
        {
            sb.Append("<div class=\"slot slot--card slot--card-face\"><img src=\"")
                .Append(PupilLeerlingSsrParts.E(PupilJourneyTheme.PuzzleMatchAsset(themeKey, face)))
                .Append("\" alt=\"\" loading=\"lazy\" decoding=\"async\" /></div>");
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    internal static string PuzzleLampPreviewRow(string? themeKey)
    {
        _ = themeKey;
        var sb = new StringBuilder("""<div class="prev-row-box prev-row-box--lamps"><div class="prev-row prev-row--lamps" aria-hidden="true">""");
        for (var i = 0; i < 3; i++)
        {
            sb.Append(PuzzleLampBeaconMarkup(i, asButton: false));
        }

        sb.Append("</div></div>");
        return sb.ToString();
    }

    internal static string PuzzleLampBeaconMarkup(int index, bool asButton, CultureState? culture = null)
    {
        var tone = index + 1;
        var glass = """<span class="ll-puzzle__beacon-glass" aria-hidden="true"></span>""";
        if (!asButton)
        {
            return $"""<div class="slot slot--lamp slot--beacon" data-ll-lamp-tone="{tone}">{glass}</div>""";
        }

        var label = culture is null
            ? $"Lamp {tone}"
            : string.Format(culture["Leerling.Puzzle.Vuurtoren.Lamp"], tone);
        return $"""
            <button type="button" class="ll-puzzle__lamp ll-puzzle__beacon" data-ll-lamp-index="{index}" data-ll-lamp-tone="{tone}" data-testid="ll-puzzle-lamp" aria-label="{PupilLeerlingSsrParts.E(label)}">{glass}<span class="ll-puzzle__lamp-glow" aria-hidden="true"></span></button>
            """;
    }

    internal static string PuzzlePatternPlay(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append(PuzzlePreviewRow(ariaHidden: false));
        sb.Append("<p class=\"ll-puzzle__hint\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Step3"])).Append("</p>");
        sb.Append("""<div class="answers ll-puzzle-schelp-choices" role="group" aria-label="Kies een schelp">""");
        foreach (var n in PupilJourneyTheme.SchelpenrijPatternChoices())
        {
            sb.Append("<button type=\"button\" class=\"ans\" data-ll-schelp-choice=\"").Append(n)
                .Append("\" data-testid=\"ll-puzzle-shell\"><img src=\"/leerling/werelden/img/schelp-")
                .Append(n).Append(".png\" alt=\"\" width=\"56\" height=\"56\" loading=\"lazy\" decoding=\"async\" /></button>");
        }

        sb.Append("</div><p class=\"ll-puzzle__status ll-puzzle__status--calm\" data-ll-puzzle-success hidden aria-live=\"polite\">")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.PatternDone"]))
            .Append("</p>");
        return sb.ToString();
    }

    internal static string PuzzleMatchPlay(string? themeKey, CultureState culture)
    {
        var back = PupilJourneyTheme.PuzzleCardBackAsset(themeKey);
        var faces = PupilJourneyTheme.PuzzleMatchPairIds(themeKey).ToList();
        var sb = new StringBuilder();
        sb.Append("<p class=\"ll-puzzle__hint\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Schatkaart.Hint"])).Append("</p>");
        sb.Append("""<div class="ll-puzzle__cards ll-puzzle__cards--g78">""");
        for (var i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            var matchSrc = PupilJourneyTheme.PuzzleMatchAsset(themeKey, face);
            sb.Append("<button type=\"button\" class=\"ll-puzzle__card\" data-ll-card-index=\"").Append(i);
            sb.Append("\" data-ll-card-face=\"").Append(PupilLeerlingSsrParts.E(face));
            sb.Append("\" data-ll-match-src=\"").Append(PupilLeerlingSsrParts.E(matchSrc));
            sb.Append("\" style=\"background-image:url('").Append(PupilLeerlingSsrParts.E(back));
            sb.Append("')\" data-testid=\"ll-puzzle-card\"></button>");
        }

        sb.Append("</div><p class=\"ll-puzzle__status ll-puzzle__status--calm\" data-ll-puzzle-success hidden aria-live=\"polite\">")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Schatkaart.Done"]))
            .Append("</p>");
        return sb.ToString();
    }

    internal static string PuzzleLampPlay(string? themeKey, CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<p class=\"ll-puzzle__hint\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Vuurtoren.Hint"])).Append("</p>");
        _ = themeKey;
        sb.Append("""<div class="ll-puzzle__lamps ll-puzzle__lamps--g78" role="group" aria-label="Vuurtorenlampen">""");
        for (var i = 0; i < 3; i++)
        {
            sb.Append(PuzzleLampBeaconMarkup(i, asButton: true, culture));
        }

        sb.Append("</div>");
        sb.Append("<p class=\"ll-puzzle__status\" data-ll-lamp-watch aria-live=\"polite\">")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Vuurtoren.Watch"])).Append("</p>");
        sb.Append("<p class=\"ll-puzzle__status\" data-ll-lamp-turn hidden aria-live=\"polite\">")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Vuurtoren.YourTurn"])).Append("</p>");
        sb.Append("""<button type="button" class="btn btn-ghost ll-puzzle__replay" data-ll-puzzle-replay hidden>""")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Replay"])).Append("</button>");
        sb.Append("<p class=\"ll-puzzle__status ll-puzzle__status--calm\" data-ll-puzzle-success hidden aria-live=\"polite\">")
            .Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Vuurtoren.Done"]))
            .Append("</p>");
        return sb.ToString();
    }

    internal static string ResolveJobKey(string title)
    {
        foreach (var job in DreamJobCatalog.All)
        {
            if (string.Equals(job.TitleNl, title, StringComparison.OrdinalIgnoreCase))
            {
                return job.Key;
            }
        }

        return DreamJobCatalog.TryResolveExactKey(title, out var key) ? key : title;
    }

    internal static string JobPickCard(
        CultureState culture,
        string jobKey,
        string title,
        string domain,
        string blurb,
        bool selected,
        string? journeyThemeKey = null,
        string? whyHtml = null)
    {
        var paintClass = PupilGroep78JobPaint.PaintThemeClass(journeyThemeKey);
        var img = PupilGroep78JobPaint.PaintImagePath(jobKey);
        _ = selected;
        var mark = """<span class="jmark">✓ Jouw keuze</span>""";
        var whyBlock = string.IsNullOrWhiteSpace(whyHtml)
            ? ""
            : $"""<p class="jwhy">{whyHtml}</p>""";
        var checkedAttr = selected ? " checked" : "";
        return $"""
            <label class="jpick{(selected ? " sel" : "")}" data-ll-job-pick="{PupilLeerlingSsrParts.E(title)}">
            <input type="radio" name="DreamJob" value="{PupilLeerlingSsrParts.E(title)}" class="visually-hidden"{checkedAttr} />
            <figure class="jpick-paint {paintClass}"><img src="{PupilLeerlingSsrParts.E(img)}" alt="" width="400" height="400" loading="lazy" /></figure>
            <div class="jbody"><p class="jtitle">{PupilLeerlingSsrParts.E(title)}</p><p class="jdom">{PupilLeerlingSsrParts.E(domain)}</p><p class="jblurb">{PupilLeerlingSsrParts.E(blurb)}</p>{whyBlock}</div>
            <span class="jspeak" onclick="event.preventDefault(); event.stopPropagation();">{PupilGroep78SsrParts.Groep78SpeakButton(blurb, $"ll-job-{jobKey}", culture)}</span>
            {mark}
            </label>
            """;
    }

    internal static string JobPickWhyHtml(CultureState culture, string jobKey, IReadOnlyList<string> likeKeys)
    {
        var beroepKey = PupilGroep78JobPaint.DisplayBeroepssoortKey(jobKey);
        var omdat = beroepKey is null ? "" : PupilG78BeroepssoortCatalog.Omdat(beroepKey, english: false);
        if (string.IsNullOrWhiteSpace(omdat))
        {
            return "";
        }

        var likeLabel = "";
        foreach (var likeKey in likeKeys)
        {
            var chip = PupilInterestChipCatalog.LikeChips.FirstOrDefault(c => c.Key == likeKey);
            if (chip is not null)
            {
                likeLabel = culture[chip.LabelKey];
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(likeLabel))
        {
            return $"Leuk voor jou omdat {char.ToLowerInvariant(omdat[0])}{omdat[1..]}";
        }

        return $"Leuk voor jou omdat jij van <b>{PupilLeerlingSsrParts.E(likeLabel.ToLowerInvariant())}</b> houdt. {omdat}";
    }

    internal static string JobPickNone(CultureState culture, bool selected)
    {
        var label = culture["Leerling.G78.Dream.None"];
        var blurb = culture["Leerling.G78.Dream.NoneBlurb"];
        _ = selected;
        var mark = """<span class="jmark">✓ Jouw keuze</span>""";
        var checkedAttr = selected ? " checked" : "";
        return $"""
            <label class="jpick jpick-none{(selected ? " sel" : "")}" data-ll-job-pick="">
            <input type="radio" name="DreamJob" value="" class="visually-hidden"{checkedAttr} />
            <div class="jpick-art jpick-art-none" aria-hidden="true">?</div>
            <div class="jbody"><p class="jtitle">{PupilLeerlingSsrParts.E(label)}</p><p class="jblurb">{PupilLeerlingSsrParts.E(blurb)}</p></div>
            <span class="jspeak" onclick="event.preventDefault(); event.stopPropagation();">{PupilGroep78SsrParts.Groep78SpeakButton(blurb, "ll-job-none", culture)}</span>
            {mark}
            </label>
            """;
    }

    internal static string ResultTile(
        CultureState culture,
        string imageStem,
        PupilStoryTileDto tile)
        => $"""
            <div class="rtile">
            <div class="rtile-img" style="background-image:url('/leerling/werelden/img/{PupilLeerlingSsrParts.E(imageStem)}.jpg')"></div>
            <div class="rtile-body"><p class="kicker">{PupilLeerlingSsrParts.E(tile.KidLabel)}</p><p class="rtile-txt">{PupilLeerlingSsrParts.E(tile.Explanation)}</p>
            {PupilGroep78SsrParts.Groep78SpeakButton(tile.Explanation, $"ll-tile-{tile.ModelKey}", culture)}
            </div></div>
            """;
}
