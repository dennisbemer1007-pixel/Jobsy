namespace Jobsy.Core.Careers;

/// <summary>Builds stored occupation days from dagteksten.json (NL source + EN translation).</summary>
public static class OccupationDayDagtekstenImport
{
    public const string SourceModel = "gpt-4o";

    public static bool TryBuild(
        OccupationDayDagtekstEntry tekst,
        OccupationDayFacts facts,
        out OccupationDayDraft dutch,
        out OccupationDayDraft english,
        out string? error)
    {
        dutch = null!;
        english = null!;
        error = null;

        if (tekst.Nl is null || tekst.En is null)
        {
            error = "taal-ontbreekt";
            return false;
        }

        if (!TryBuildLocale(tekst.Nl, facts.TitleNl, out dutch, out error))
        {
            return false;
        }

        if (!TryBuildLocale(tekst.En, facts.TitleNl, out english, out error))
        {
            return false;
        }

        if (dutch.Blocks!.Count != english.Blocks!.Count)
        {
            error = "blok-aantal";
            return false;
        }

        for (var i = 0; i < dutch.Blocks.Count; i++)
        {
            if (!string.Equals(dutch.Blocks[i].Key, english.Blocks[i].Key, StringComparison.Ordinal))
            {
                error = "blok-key";
                return false;
            }
        }

        if (!OccupationDayInLifeValidator.TryValidateTranslation(dutch, english, out _))
        {
            error = "vertaling";
            return false;
        }

        return true;
    }

    private static bool TryBuildLocale(
        OccupationDayDagtekstLocale locale,
        string catalogTitle,
        out OccupationDayDraft draft,
        out string? error)
    {
        draft = null!;
        error = null;
        var blokken = locale.Blokken ?? [];
        if (blokken.Count is < OccupationDayTimeline.DagtekstenMinBlocks or > OccupationDayTimeline.DagtekstenMaxBlocks)
        {
            error = "blok-aantal";
            return false;
        }

        var blocks = new List<OccupationDayBlock>(blokken.Count);
        for (var i = 0; i < blokken.Count; i++)
        {
            var blok = blokken[i];
            var tijd = (blok.Tijd ?? "").Trim();
            var kop = (blok.Kop ?? "").Trim();
            var tekst = (blok.Tekst ?? "").Trim();
            if (tijd.Length == 0 || kop.Length == 0 || tekst.Length == 0)
            {
                error = "blok-leeg";
                return false;
            }

            var label = tijd + " " + kop;
            if (label.Length > 32)
            {
                label = label[..32].Trim();
            }

            blocks.Add(new OccupationDayBlock((i + 1).ToString(), label, tekst));
        }

        if (!OccupationDayTimeline.TryValidateDagteksten(blocks, out var timelineReason))
        {
            error = timelineReason ?? "tijdlijn";
            return false;
        }

        blocks = OccupationDayBlocks.NormalizeNumbered(blocks).ToList();
        var highlights = HighlightsFrom(locale.Intro, locale.Slot);
        var varies = VariesFrom(locale.Slot);
        var title = string.IsNullOrWhiteSpace(locale.Titel) ? catalogTitle : locale.Titel.Trim();
        draft = OccupationDayBlocks.WithDerived(new OccupationDayDraft(
            title,
            "",
            "",
            "",
            "",
            highlights,
            varies,
            blocks));
        return true;
    }

    private static List<string> HighlightsFrom(string intro, string slot)
    {
        var list = new List<string>();
        foreach (var sentence in SplitSentences(intro))
        {
            if (list.Count >= 4)
            {
                break;
            }

            var line = TrimHighlight(sentence);
            if (line.Length >= 8 && line.Length <= 140 && OccupationDayInLifeValidator.WordCount(line) >= 2)
            {
                list.Add(line);
            }
        }

        if (list.Count < 2)
        {
            foreach (var sentence in SplitSentences(slot))
            {
                if (list.Count >= 2)
                {
                    break;
                }

                var line = TrimHighlight(sentence);
                if (line.Length >= 8 && line.Length <= 140 && !list.Contains(line, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(line);
                }
            }
        }

        while (list.Count < 2)
        {
            list.Add(list.Count == 0 ? "Je werkt met taken uit dit beroep." : "De volgorde kan per dag verschillen.");
        }

        return list.Take(4).ToList();
    }

    private static string VariesFrom(string slot)
    {
        var text = (slot ?? "").Trim();
        if (text.Length >= 20
            && text.Length <= 400
            && (text.Contains("werkgever", StringComparison.OrdinalIgnoreCase)
                || text.Contains("verschil", StringComparison.OrdinalIgnoreCase)
                || text.Contains("per ", StringComparison.OrdinalIgnoreCase)
                || text.Contains("soms", StringComparison.OrdinalIgnoreCase)))
        {
            return text;
        }

        return "De volgorde en taken kunnen per werkgever verschillen. Dat is normaal in dit beroep.";
    }

    private static IEnumerable<string> SplitSentences(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var chunk = text.Trim();
        foreach (var part in chunk.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length > 0)
            {
                yield return part + ".";
            }
        }
    }

    private static string TrimHighlight(string line)
    {
        var t = line.Trim().TrimEnd('.');
        if (t.Length == 0)
        {
            return "";
        }

        return char.ToUpper(t[0]) + t[1..] + ".";
    }
}
