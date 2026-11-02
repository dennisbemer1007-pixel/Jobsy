using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Scholen;

internal static class PupilGroep78MockHtml
{
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

    internal static string PuzzlePreviewRow()
    {
        var shells = new[] { 1, 2, 2, 3, 1 };
        var sb = new StringBuilder("""<div class="prev-row" aria-hidden="true">""");
        foreach (var n in shells)
        {
            sb.Append("<div class=\"slot\"><img src=\"/leerling/werelden/img/schelp-").Append(n).Append(".png\" alt=\"\" /></div>");
        }

        sb.Append("""<div class="slot q">?</div></div>""");
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
        bool selected)
    {
        var job = DreamJobCatalog.All.FirstOrDefault(j => string.Equals(j.Key, jobKey, StringComparison.OrdinalIgnoreCase));
        var icon = job is null ? DreamJobIcons.Briefcase : DreamJobIcons.TryGet(job.IconKey) ?? DreamJobIcons.Briefcase;
        var mark = selected
            ? """<span class="jmark">✓ Jouw keuze</span>"""
            : "";
        return $"""
            <button type="button" class="jpick{(selected ? " sel" : "")}" data-ll-job-pick="{PupilLeerlingSsrParts.E(title)}" aria-pressed="{selected.ToString().ToLowerInvariant()}">
            <span class="jico">{icon}</span>
            <div class="jbody"><p class="jtitle">{PupilLeerlingSsrParts.E(title)}</p><p class="jdom">{PupilLeerlingSsrParts.E(domain)}</p><p class="jblurb">{PupilLeerlingSsrParts.E(blurb)}</p></div>
            <span class="jspeak">{PupilGroep78SsrParts.Groep78SpeakButton(blurb, $"ll-job-{jobKey}", culture)}</span>
            {mark}
            </button>
            """;
    }

    internal static string JobPickNone(CultureState culture, bool selected)
    {
        var label = culture["Leerling.G78.Dream.None"];
        var blurb = culture["Leerling.G78.Dream.NoneBlurb"];
        var mark = selected ? """<span class="jmark">✓ Jouw keuze</span>""" : "";
        return $"""
            <button type="button" class="jpick jpick-none{(selected ? " sel" : "")}" data-ll-job-pick="" aria-pressed="{selected.ToString().ToLowerInvariant()}">
            <div class="jpick-art jpick-art-none" aria-hidden="true">?</div>
            <div class="jbody"><p class="jtitle">{PupilLeerlingSsrParts.E(label)}</p><p class="jblurb">{PupilLeerlingSsrParts.E(blurb)}</p></div>
            <span class="jspeak">{PupilGroep78SsrParts.Groep78SpeakButton(blurb, "ll-job-none", culture)}</span>
            {mark}
            </button>
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
