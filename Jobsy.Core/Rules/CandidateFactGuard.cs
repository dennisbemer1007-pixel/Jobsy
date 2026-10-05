using System.Text.RegularExpressions;
using Jobsy.Core.Careers;

namespace Jobsy.Core.Rules;

/// <summary>
/// Rejects candidate-facing AI text that invents work, employers, years, diplomas,
/// job titles outside the catalogue or result list, or markdown.
/// </summary>
public static partial class CandidateFactGuard
{
    public const string StrictAddendum =
        "STRIKT: Gebruik alleen de feitenlijst. Verzin geen werkgever, sector, jaartal, diploma of ervaring. " +
        "Staat er werkervaring: geen, zeg niets over eerder werk. " +
        "Noem alleen beroepstitels uit de toegestane lijst. Geen markdown. " +
        "Ontbreekt een feit, zeg dat je het niet weet.";

    public static string? RejectionReason(string? text, CandidateFactSheet sheet)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "empty";
        }

        if (Markdown().IsMatch(text))
        {
            return "markdown";
        }

        if (MentionsUnknownWork(text, sheet))
        {
            return "invented-work";
        }

        if (MentionsUnknownDiploma(text, sheet))
        {
            return "unknown-diploma";
        }

        if (MentionsUnknownYear(text, sheet))
        {
            return "unknown-year";
        }

        if (MentionsUnknownPlace(text, sheet))
        {
            return "invented-place";
        }

        if (MentionsInventedLike(text, sheet))
        {
            return "invented-like";
        }

        if (MentionsStatedLike(text))
        {
            return "invented-like";
        }

        if (sheet.CheckJobTitles && MentionsUnknownJob(text, sheet))
        {
            return "unknown-job";
        }

        if (DeniesOwnDirection(text))
        {
            return "denied-own-direction";
        }

        if (MentionsUnsourcedOutlook(text, sheet))
        {
            return "unknown-outlook";
        }

        if (MentionsUnsourcedAdvice(text, sheet))
        {
            return "unknown-advice";
        }

        return null;
    }

    /// <summary>One Dutch sentence for a second model attempt. No profile text.</summary>
    public static string ReasonSentence(string? reason) => reason switch
    {
        "invented-work" => "De vorige tekst verzon werk of een sector. Laat dat weg.",
        "unknown-diploma" => "Noem alleen het opleidingsniveau dat in de feiten staat.",
        "unknown-year" => "Noem geen jaartal of aantal jaren dat niet in de feiten staat.",
        "invented-place" => "Noem geen woonplaats of regio. Die staat niet in de feiten.",
        "invented-like" => "Zeg niet wat de persoon leuk vindt. Dat staat niet in de feiten.",
        "unknown-job" => "Noem alleen een beroep uit de toegestane lijst.",
        "unknown-percent" => "Gebruik alleen een percentage uit de feitenlijst.",
        "denied-own-direction" => "Zeg niet dat een beroep een eigen richting niet heeft.",
        "why-no-direction" => "Elke why-zin noemt één richting uit de feitenlijst.",
        "markdown" => "Geen markdown. Alleen gewone zinnen.",
        "story-rules" => "Schrijf 2 tot 4 alinea's in de ik-vorm, zonder herhaling.",
        "paragraphs" => "Schrijf 2 tot 4 alinea's. Zet een lege regel tussen de alinea's. In de JSON-string is die lege regel \\n\\n.",
        "claimed-completed" => "Zeg niet dat een opleiding is afgerond. Dat staat niet in de feiten.",
        "unknown-outlook" => "Citeer alleen een vooruitblikzin uit de feitenlijst. Verzin geen andere zin over 2030 of AI.",
        "unknown-advice" => "Citeer alleen een eerlijk advies uit de feitenlijst. Verzin geen ander advies over het beroep of over AI.",
        _ => "Gebruik alleen de feitenlijst. Verzin niets."
    };

    public static string WithoutInventedHistory(string? text, CandidateFactSheet sheet, string fallback)
        => RejectionReason(text, sheet) is null ? text!.Trim() : fallback;

    /// <summary>True when every title maps onto the labour-market catalogue.</summary>
    public static bool IsCatalogueTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var parts = TitleParts(title);
        return parts.Count > 0 && parts.TrueForAll(part => CareerCompassSanitize.CanonicalTitle(part) is not null);
    }

    public static string? CompassRejection(string? rawJson, CandidateFactSheet sheet)
    {
        var dto = CareerCompassJson.TryRead(rawJson);
        if (dto is null)
        {
            return "unreadable";
        }

        var titles = (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? [])
            .Select(item => item.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Cast<string>()
            .ToList();
        if (titles.Count == 0)
        {
            return "no-jobs";
        }

        foreach (var title in titles)
        {
            foreach (var part in TitleParts(title))
            {
                var canonical = CareerCompassSanitize.CanonicalTitle(part);
                if (canonical is null)
                {
                    return "unknown-job";
                }

                if (sheet.AllowedJobTitles.Count > 0 && !sheet.AllowsJob(canonical) && !sheet.AllowsJob(part))
                {
                    return "unknown-job";
                }
            }
        }

        var proseSheet = CandidateFactSheet.ForCareerProse(sheet.AllowedJobTitles);
        if (dto.Strengths is not null)
        {
            foreach (var line in dto.Strengths)
            {
                var reason = RejectionReason(line, proseSheet);
                if (reason is not null)
                {
                    return reason;
                }
            }
        }

        if (dto.PracticalNotes is not null)
        {
            foreach (var line in dto.PracticalNotes)
            {
                // A place name in one sentence is dropped later. Other invented facts still fail the reply.
                var reason = NoteRejection(line, proseSheet);
                if (reason is not null)
                {
                    return reason;
                }
            }
        }

        foreach (var item in (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? []))
        {
            if (string.IsNullOrWhiteSpace(item.Why))
            {
                continue;
            }

            var whyReason = RejectionReason(item.Why, proseSheet);
            if (whyReason is not null)
            {
                return whyReason;
            }

            if (!WhyNamesOwnDirection(item.Title, item.Why, sheet.DirectionLabels))
            {
                return "why-no-direction";
            }

            if (!JobPercentAllowed(item.Title, item.Percent, sheet))
            {
                return "unknown-percent";
            }
        }

        return null;
    }

    /// <summary>
    /// Drops sentences the fact guard rejects. A note can keep "Open de banenkaart."
    /// when a later sentence names a place that is not on the fact sheet.
    /// </summary>
    internal static string? WithoutRejectedSentences(string? text, CandidateFactSheet sheet)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var kept = SplitSentences(text)
            .Where(sentence => RejectionReason(sentence, sheet) is null)
            .ToList();
        return kept.Count == 0 ? null : string.Join(" ", kept);
    }

    private static string? NoteRejection(string? text, CandidateFactSheet sheet)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var sentence in SplitSentences(text))
        {
            var reason = RejectionReason(sentence, sheet);
            if (reason is not null && reason != "invented-place")
            {
                return reason;
            }
        }

        return null;
    }

    private static bool MentionsUnsourcedOutlook(string text, CandidateFactSheet sheet)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (!LooksLikeOutlook(sentence))
            {
                continue;
            }

            var trimmed = sentence.Trim();
            if (!QuoteAllowed(trimmed, sheet.OutlookLines) && !QuoteAllowed(trimmed, sheet.HonestAdviceLines))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MentionsUnsourcedAdvice(string text, CandidateFactSheet sheet)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (!LooksLikeHonestAdvice(sentence))
            {
                continue;
            }

            if (!QuoteAllowed(sentence.Trim(), sheet.HonestAdviceLines))
            {
                return true;
            }
        }

        return false;
    }

    private static bool QuoteAllowed(string trimmed, IReadOnlyList<string> lines)
        => lines.Any(line =>
            line.Contains(trimmed, StringComparison.Ordinal)
            || trimmed.Contains(line, StringComparison.Ordinal));

    private static bool LooksLikeHonestAdvice(string sentence)
        => sentence.Contains("eerlijk advies", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("met AI", StringComparison.Ordinal)
           || sentence.Contains("AI verandert", StringComparison.Ordinal)
           || sentence.Contains("te weinig gegevens voor een advies", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("with AI", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("AI changes", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("honest advice", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("przez AI", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("z AI", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("din cauza AI", StringComparison.OrdinalIgnoreCase)
           || sentence.Contains("الذكاء الاصطناعي", StringComparison.Ordinal);

    private static bool LooksLikeOutlook(string sentence)
        => sentence.Contains("Tot 2030", StringComparison.Ordinal)
           || sentence.Contains("van elke 100 banen", StringComparison.Ordinal)
           || sentence.Contains("door AI", StringComparison.Ordinal)
           || sentence.Contains("door computers", StringComparison.Ordinal)
           || sentence.Contains("verandert AI", StringComparison.Ordinal)
           || sentence.Contains("Dat weten we niet: de ILO", StringComparison.Ordinal)
           || sentence.Contains("Dat weten we niet: er zijn geen cijfers", StringComparison.Ordinal)
           || sentence.Contains("blijft mensenwerk", StringComparison.Ordinal)
           || sentence.Contains("iets extra's te leren", StringComparison.Ordinal)
           || sentence.Contains("geen beroep gevonden dat dichtbij ligt", StringComparison.Ordinal);

    private static bool MentionsUnknownWork(string text, CandidateFactSheet sheet)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (Negated().IsMatch(sentence))
            {
                continue;
            }

            var past = PastWork().IsMatch(sentence);
            var sector = SectorWord(sentence);
            var sectorClaim = sector is not null && SectorPhrase().IsMatch(sentence);
            if (!past && !sectorClaim)
            {
                continue;
            }

            if (past && !sheet.HasWorkExperience)
            {
                return true;
            }

            if (past && sheet.HasWorkExperience && !SentenceUsesKnownRole(sentence, sheet))
            {
                return true;
            }

            if (sectorClaim && sheet.PersonalHistory && sector is not null && !sheet.HistoryContains(sector))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SentenceUsesKnownRole(string sentence, CandidateFactSheet sheet)
    {
        foreach (var role in sheet.WorkExperience)
        {
            foreach (var word in role.Split([' ', ',', '(', ')', '-'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length >= 4 && sentence.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MentionsUnknownDiploma(string text, CandidateFactSheet sheet)
    {
        if (CompletedClaim().IsMatch(text) && !sheet.HistoryContains("afgerond"))
        {
            return true;
        }

        var education = string.Join(" ", sheet.Education);
        foreach (Match match in LevelToken().Matches(text))
        {
            var around = Around(text, match.Index, match.Length);
            if (!EducationAllowsLevel(education, match.Value, around))
            {
                return true;
            }
        }

        if (DiplomaWord().IsMatch(text) && sheet.Education.Count == 0)
        {
            return true;
        }

        return false;
    }

    private static bool EducationAllowsLevel(string education, string token, string context)
    {
        if (string.IsNullOrWhiteSpace(education))
        {
            return false;
        }

        var edu = education.ToLowerInvariant();
        var level = token.ToLowerInvariant();
        if (level == "wo")
        {
            return Regex.IsMatch(edu, @"\bwo\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                   || edu.Contains("universit", StringComparison.Ordinal);
        }

        if (!edu.Contains(level, StringComparison.Ordinal))
        {
            return false;
        }

        if (level != "mbo")
        {
            return true;
        }

        var textLevel = MboNumber(context);
        var eduLevel = MboNumber(edu);
        if (textLevel is int wanted && eduLevel is int have && wanted != have)
        {
            return false;
        }

        return textLevel is null || eduLevel is not null;
    }

    private static int? MboNumber(string text)
    {
        var match = Regex.Match(
            text,
            @"mbo(?:\s*-\s*|\s+niveau\s+|\s+)([1-4])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var level) ? level : null;
    }

    private static string Around(string text, int index, int length)
    {
        var start = Math.Max(0, index - 12);
        var end = Math.Min(text.Length, index + length + 16);
        return text[start..end];
    }

    private static bool MentionsUnknownPlace(string text, CandidateFactSheet sheet)
    {
        foreach (var place in PlaceNames)
        {
            if (!ContainsPhrase(text, place))
            {
                continue;
            }

            if (sheet.HistoryContains(place) || PlaceMatchesCity(place, sheet.HomeCity))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool PlaceMatchesCity(string place, string? city)
        => !string.IsNullOrWhiteSpace(city)
           && (city.Contains(place, StringComparison.OrdinalIgnoreCase)
               || place.Contains(city, StringComparison.OrdinalIgnoreCase));

    private static bool MentionsInventedLike(string text, CandidateFactSheet sheet)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (Negated().IsMatch(sentence) || !LikeClaim().IsMatch(sentence))
            {
                continue;
            }

            foreach (var word in LikeObjects)
            {
                if (!ContainsPhrase(sentence, word))
                {
                    continue;
                }

                if (!sheet.HistoryContains(word))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// A reply that says job X does not match direction D, while D is one of X's catalogue letters.
    /// A line about the person's own top directions ("zit niet bij je hoogste") stays.
    /// </summary>
    private static bool DeniesOwnDirection(string text)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (IsPersonTopDenial(sentence))
            {
                continue;
            }

            foreach (var title in CandidateJobAdvice.TitlesIn(sentence))
            {
                if (!NegatesJob(sentence, title))
                {
                    continue;
                }

                foreach (var weight in CareerCompassBuilder.WeightsFor(title))
                {
                    if (SentenceNamesDirection(sentence, weight.Code))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsPersonTopDenial(string sentence)
    {
        var fold = sentence.ToLowerInvariant();
        return fold.Contains("hoogste", StringComparison.Ordinal)
               || fold.Contains("top 3", StringComparison.Ordinal)
               || fold.Contains("top3", StringComparison.Ordinal)
               || fold.Contains("your top", StringComparison.Ordinal)
               || fold.Contains("najwyższ", StringComparison.Ordinal)
               || fold.Contains("najwyzsz", StringComparison.Ordinal)
               || fold.Contains("primele", StringComparison.Ordinal)
               || fold.Contains("أعلى", StringComparison.Ordinal);
    }

    private static bool NegatesJob(string sentence, string title)
    {
        var index = sentence.IndexOf(title, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            var head = title.Split('/')[0].Trim();
            index = head.Length >= 4
                ? sentence.IndexOf(head, StringComparison.OrdinalIgnoreCase)
                : -1;
            if (index < 0)
            {
                return false;
            }

            title = head;
        }

        var start = Math.Max(0, index - 12);
        var end = Math.Min(sentence.Length, index + title.Length + 16);
        var window = sentence[start..end];
        return JobNegation().IsMatch(window);
    }

    private static bool SentenceNamesDirection(string sentence, string code)
    {
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var label = DimensionLabels.For(code, lang);
            if (label.Length >= 4 && sentence.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var dutch = CareerCompassBuilder.TypeLabel(code);
        return dutch.Length >= 4 && sentence.Contains(dutch, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\b(niet|not|nie|nu|doesn't|does not)\b|لا|ليس", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JobNegation();

    private static bool NamesDirection(string why, IReadOnlyList<string> labels)
        => labels.Any(label =>
            label.Length > 0 && why.Contains(label, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A why-line may name the job's own direction ("Chauffeur → Aanpakken met je handen")
    /// or one of the candidate's top direction labels.
    /// </summary>
    private static bool WhyNamesOwnDirection(string? title, string why, IReadOnlyList<string> labels)
    {
        if (labels.Count == 0 || NamesDirection(why, labels))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return labels.Count == 0;
        }

        var canonical = CareerCompassSanitize.CanonicalTitle(title) ?? title;
        var own = CareerCompassBuilder.TypeLabel(CareerCompassBuilder.PrimaryCode(canonical));
        return own.Length > 0 && why.Contains(own, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Rejects a positive claim about a trait that scores under 50.
    /// The label or a short alias must appear next to praise.
    /// </summary>
    public static string? BelowAveragePraise(string? text, IReadOnlyList<(string Label, int Score)> traits)
    {
        if (string.IsNullOrWhiteSpace(text) || traits.Count == 0)
        {
            return null;
        }

        foreach (var (label, score) in traits)
        {
            if (score > 50 || string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            foreach (var sentence in SplitSentences(text))
            {
                if (Negated().IsMatch(sentence) || TestOutcome().IsMatch(sentence))
                {
                    continue;
                }

                if (!SentenceNamesTrait(sentence, label))
                {
                    continue;
                }

                if (Praise().IsMatch(sentence))
                {
                    return "below-average";
                }
            }
        }

        return null;
    }

    private static bool SentenceNamesTrait(string sentence, string label)
    {
        if (sentence.Contains(label, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var key = label.ToLowerInvariant();
        if (key.Contains("nieuw", StringComparison.Ordinal)
            && (sentence.Contains("nieuwe idee", StringComparison.OrdinalIgnoreCase)
                || sentence.Contains("open voor", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (key.Contains("kalm", StringComparison.Ordinal)
            && (sentence.Contains("hoofd koel", StringComparison.OrdinalIgnoreCase)
                || sentence.Contains("blijft kalm", StringComparison.OrdinalIgnoreCase)
                || sentence.Contains("rustig", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// "You like / je werkt graag / تحب" is a stated personal like.
    /// A test outcome ("uit je test blijkt", "je scoort hoog") may stay.
    /// </summary>
    private static bool MentionsStatedLike(string text)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (Negated().IsMatch(sentence) || TestOutcome().IsMatch(sentence))
            {
                continue;
            }

            if (StatedLike().IsMatch(sentence))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MentionsUnknownYear(string text, CandidateFactSheet sheet)
    {
        foreach (Match match in YearClaim().Matches(text))
        {
            if (!sheet.HistoryContains(match.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool JobPercentAllowed(string? title, decimal percent, CandidateFactSheet sheet)
    {
        var publishes = sheet.Scores.Any(line => line.Contains('→') && line.Contains('%'));
        if (!publishes)
        {
            return true;
        }

        var canonical = CareerCompassSanitize.CanonicalTitle(title) ?? title?.Trim() ?? "";
        if (canonical.Length == 0)
        {
            return false;
        }

        return sheet.Scores.Any(line =>
            line.Contains(canonical, StringComparison.OrdinalIgnoreCase)
            && line.Contains($": {CareerCompassBuilder.FormatPercent(percent)}%", StringComparison.Ordinal));
    }

    private static bool MentionsUnknownJob(string text, CandidateFactSheet sheet)
    {
        foreach (var phrase in JobPhrases.Value)
        {
            if (!ContainsPhrase(text, phrase))
            {
                continue;
            }

            if (!sheet.AllowsJob(phrase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsPhrase(string text, string phrase)
    {
        var index = 0;
        while ((index = text.IndexOf(phrase, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var end = index + phrase.Length;
            var after = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (before && after)
            {
                return true;
            }

            index = end;
        }

        return false;
    }

    private static string? SectorWord(string sentence)
    {
        var match = SectorPhrase().Match(sentence);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<string> TitleParts(string raw)
    {
        var trimmed = raw.Trim().Trim('*', '"', '\'', '«', '»');
        if (trimmed.Length == 0)
        {
            return [];
        }

        if (CareerCompassSanitize.CanonicalTitle(trimmed) is not null)
        {
            return [trimmed];
        }

        var parts = ComboSplit().Split(trimmed);
        if (parts.Length <= 1)
        {
            return [trimmed];
        }

        return parts
            .Select(part => part.Trim().Trim('*', '"', '\''))
            .Where(part => part.Length > 0)
            .ToList();
    }

    private static List<string> SplitSentences(string text)
        => Regex.Split(text, @"(?<=[\.!\?\n])")
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

    private static readonly Lazy<string[]> JobPhrases = new(BuildJobPhrases);

    private static string[] BuildJobPhrases()
    {
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? phrase, int minLength)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                return;
            }

            var trimmed = phrase.Trim();
            if (trimmed.Length >= minLength)
            {
                phrases.Add(trimmed);
            }
        }

        foreach (var occupation in OccupationCatalog.Shared.All)
        {
            Add(occupation.Nl, 4);
        }

        foreach (var entry in CareerDreamCatalog.All)
        {
            Add(entry.Title, 5);
            foreach (var alias in entry.Aliases)
            {
                Add(alias, 8);
            }
        }

        return phrases.OrderByDescending(phrase => phrase.Length).ToArray();
    }

    [GeneratedRegex(@"\*\*|__|(?m)^[ \t]*[-*]\s+\S|(?m)^#{1,6}\s|\[[^\]]+\]\([^)]+\)", RegexOptions.CultureInvariant)]
    private static partial Regex Markdown();

    [GeneratedRegex(@"jarenlang|\bgewerkt\b|ervaring\s+in\s+de|ervaring\s+met|ervaring\s+als|\bgestaan\b|in\s+dienst\s+bij|mijn\s+werkgever", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PastWork();

    [GeneratedRegex(@"\bin\s+de\s+(zorg|bouw|horeca|kas|magazijn|keuken|logistiek|onderwijs|transport|techniek|winkel|kantoor|landbouw|schoonmaak)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SectorPhrase();

    [GeneratedRegex(@"\b(mbo|hbo|wo|vwo|havo|vmbo|bachelor|master|universiteit)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LevelToken();

    [GeneratedRegex(@"\b(diploma|opleiding)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiplomaWord();

    [GeneratedRegex(@"\bafgerond\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CompletedClaim();

    [GeneratedRegex(@"\b(vind|vindt|graag|hart|enthousiast|leuk|fijn|houd van|houdt van)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LikeClaim();

    [GeneratedRegex(
        @"\byou like\b|\byou enjoy\b|\byou love\b|\bje werkt graag\b|\bje houdt van\b|\bje vind(?:t)? .+ leuk\b|\blubisz\b|\bîți place\b|\biti place\b|تحب",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StatedLike();

    [GeneratedRegex(
        @"uit je test|je scoort|your test|you score|wynik|scorul tău|scorul tau|اختبار|bij je past|past bij je",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TestOutcome();

    [GeneratedRegex(
        @"\b(sterk|open voor|graag|energie|koel|rustig|liefde|talent|goed in)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Praise();

    private static readonly string[] LikeObjects =
    [
        "dieren", "planten", "koken", "kook", "schoonmaak", "schoonmaken", "tuinieren", "bloemen"
    ];

    /// <summary>Place names from the regional hosts. A story may use one only when it is on the fact sheet.</summary>
    internal static readonly string[] PlaceNames =
    [
        "Den Haag",
        "Westland",
        "Delft",
        "Rotterdam",
        "Naaldwijk",
        "Honselersdijk",
        "Poeldijk",
        "Wateringen",
        "Maasdijk",
        "Kwintsheul",
        "'s-Gravenzande",
        "Heenweg",
        "De Lier",
        "Rijswijk",
        "Zoetermeer",
        "Scheveningen",
        "Leidschendam",
        "Voorburg"
    ];

    [GeneratedRegex(@"jarenlang|\b\d{1,2}\s+jaar\b|\b(?:19|20)\d{2}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YearClaim();

    [GeneratedRegex(@"\b(geen|niet|nooit|weet ik niet|staat niet|do not know|don't know)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Negated();

    [GeneratedRegex(@"\s+of\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ComboSplit();
}
