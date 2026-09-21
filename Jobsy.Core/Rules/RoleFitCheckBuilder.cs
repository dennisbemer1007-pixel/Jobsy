namespace Jobsy.Core.Rules;

/// <summary>
/// Local Functie-Fit Checker when OpenAI is unavailable.
/// Candidate-facing strings stay in Jip-en-Janneke Dutch (no RIASEC/OCEAN jargon).
/// </summary>
public static class RoleFitCheckBuilder
{
    public const int MinTitleLength = 2;
    public const int MaxTitleLength = 80;

    public static string? NormalizeTitle(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var title = raw.Trim();
        if (title.Contains('@', StringComparison.Ordinal) || title.Contains("http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (title.Length < MinTitleLength)
        {
            return null;
        }

        if (title.Length > MaxTitleLength)
        {
            title = title[..MaxTitleLength].Trim();
        }

        return CareerCompassBuilder.ContainsForbiddenJargon(title) ? null : title;
    }

    public static RoleFitCheckSnapshot Build(
        string jobTitle,
        CompetencyScores competencies,
        RiasecScores career,
        bool fromDeepAnalysis)
    {
        var title = NormalizeTitle(jobTitle) ?? "deze functie";
        var occupation = FindClosestOccupation(title);
        var interestPercent = occupation is null
            ? Average(career)
            : CareerCompassBuilder.Score(occupation, career).Percent;
        var competencePercent = CompetenceFit(occupation, competencies);
        var percent = (int)Math.Clamp(
            Math.Round(interestPercent * 0.65 + competencePercent * 0.35, MidpointRounding.AwayFromZero),
            0,
            100);
        if (fromDeepAnalysis)
        {
            percent = Math.Min(100, percent + 2);
        }

        var keys = CareerOccupationKeys.Merge(title, occupation is null ? null : CareerOccupationKeys.FromTitle(occupation.Title));
        var strengths = BuildStrengths(title, occupation, competencies, career);
        var gaps = BuildGaps(occupation, competencies, career);
        var steps = BuildSteps(title, gaps, fromDeepAnalysis);
        var similar = RoleFitFunnel.SuggestSimilar(title, career);

        return Sanitize(new RoleFitCheckSnapshot(
            title,
            percent,
            strengths,
            gaps,
            steps,
            keys,
            fromDeepAnalysis,
            FromOpenAi: false,
            SimilarRoles: similar));
    }

    public static RoleFitVacancyFit BuildVacancyFit(
        Guid vacancyId,
        VacancyBarrierRequirements requirements,
        VacancyBarrierCheck formal,
        CultureFitResult? culture,
        bool availabilityOk)
    {
        var cultureOk = culture is null || culture.Percent >= CultureFitBuilder.MidThreshold;
        var formalGaps = formal.Items.Any(i => !i.Met);
        return new RoleFitVacancyFit(
            vacancyId,
            requirements.Barrier.ToString(),
            culture?.Percent,
            culture?.Band,
            culture?.Label,
            culture?.Why,
            formal.Items,
            availabilityOk,
            VacancyBarrierCatalog.ShowFormalBlock(requirements),
            availabilityOk && cultureOk && formalGaps);
    }

    public static RoleFitCheckSnapshot Sanitize(RoleFitCheckSnapshot snapshot)
    {
        var title = NormalizeTitle(snapshot.JobTitle) ?? "deze functie";
        var percent = Math.Clamp(snapshot.MatchPercent, 0, 100);
        return new RoleFitCheckSnapshot(
            title,
            percent,
            CleanList(snapshot.Strengths, 5),
            CleanList(snapshot.Gaps, 5),
            CleanList(snapshot.ActionSteps, 5),
            CareerOccupationKeys.Merge(title, snapshot.SearchKeys),
            snapshot.FromDeepAnalysis,
            snapshot.FromOpenAi,
            snapshot.VacancyFit is null ? null : SanitizeVacancy(snapshot.VacancyFit),
            RoleFitFunnel.MergeSimilar(snapshot.SimilarRoles, null));
    }

    private static RoleFitVacancyFit SanitizeVacancy(RoleFitVacancyFit fit)
    {
        var items = fit.FormalItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Label))
            .Select(i => i with
            {
                Label = i.Label.Trim(),
                Note = string.IsNullOrWhiteSpace(i.Note) || CareerCompassBuilder.ContainsForbiddenJargon(i.Note)
                    ? (i.Met ? "Dit klopt met je profiel." : "Dit is nog een gat.")
                    : i.Note.Trim()
            })
            .Where(i => !CareerCompassBuilder.ContainsForbiddenJargon(i.Label))
            .Take(12)
            .ToList();
        var why = string.IsNullOrWhiteSpace(fit.CultureWhy) || CareerCompassBuilder.ContainsForbiddenJargon(fit.CultureWhy)
            ? "Kijk of de sfeer van het team bij jou past."
            : fit.CultureWhy.Trim();
        return fit with
        {
            CultureWhy = why,
            FormalItems = items,
            CulturePercent = fit.CulturePercent is int p ? Math.Clamp(p, 0, 100) : null
        };
    }

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string>? items, int take)
    {
        if (items is null)
        {
            return [];
        }

        return items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => !CareerCompassBuilder.ContainsForbiddenJargon(s))
            .Where(s => !s.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private static CareerOccupation? FindClosestOccupation(string title)
    {
        CareerOccupation? best = null;
        var bestHits = 0;
        var foldedTitle = CareerOccupationKeys.Fold(title);
        foreach (var job in CareerCompassBuilder.Occupations)
        {
            var keys = CareerOccupationKeys.FromTitle(job.Title);
            var hits = keys.Count(key => CareerOccupationKeys.Hits(foldedTitle, key));
            if (CareerOccupationKeys.Hits(foldedTitle, CareerOccupationKeys.Fold(job.Title)))
            {
                hits += 3;
            }

            if (hits > bestHits)
            {
                bestHits = hits;
                best = job;
            }
        }

        return bestHits > 0 ? best : null;
    }

    private static int Average(RiasecScores scores)
    {
        var values = CareerTestCatalog.RiasecCodes.Select(scores.Get).OrderByDescending(v => v).Take(2).ToList();
        return values.Count == 0 ? 50 : (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
    }

    private static int CompetenceFit(CareerOccupation? occupation, CompetencyScores competencies)
    {
        var needed = NeededCompetencies(occupation);
        if (needed.Count == 0)
        {
            return AverageCompetence(competencies);
        }

        return (int)Math.Round(needed.Select(competencies.Get).Average(), MidpointRounding.AwayFromZero);
    }

    private static int AverageCompetence(CompetencyScores competencies)
        => (int)Math.Round(
            CompetencyTestCatalog.CategoryCodes.Select(competencies.Get).Average(),
            MidpointRounding.AwayFromZero);

    private static IReadOnlyList<string> NeededCompetencies(CareerOccupation? occupation)
    {
        if (occupation is null)
        {
            return CompetencyTestCatalog.CategoryCodes;
        }

        var top = occupation.Weights.OrderByDescending(w => w.Weight).Select(w => w.Code).FirstOrDefault();
        return top switch
        {
            CareerTestCatalog.Social => [CompetencyTestCatalog.Samenwerken, CompetencyTestCatalog.Stressbestendigheid],
            CareerTestCatalog.Enterprising => [CompetencyTestCatalog.Samenwerken, CompetencyTestCatalog.Resultaatgerichtheid],
            CareerTestCatalog.Investigative => [CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Resultaatgerichtheid],
            CareerTestCatalog.Artistic => [CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Samenwerken],
            CareerTestCatalog.Conventional => [CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid],
            _ => [CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid]
        };
    }

    private static List<string> BuildStrengths(
        string title,
        CareerOccupation? occupation,
        CompetencyScores competencies,
        RiasecScores career)
    {
        var lines = new List<string>();
        foreach (var category in CompetencyTestCatalog.CategoryCodes)
        {
            var value = competencies.Get(category);
            if (value >= 70)
            {
                lines.Add($"{CompetenceLabel(category)} zit al stevig: {value}%. Dat helpt in {title.ToLowerInvariant()}.");
            }
        }

        var topCareer = CareerTestCatalog.RiasecCodes
            .Select(code => (Code: code, Percent: career.Get(code)))
            .OrderByDescending(x => x.Percent)
            .First();
        if (topCareer.Percent >= 65)
        {
            lines.Add($"Jouw richting {CareerCompassBuilder.TypeLabel(topCareer.Code).ToLowerInvariant()} ({topCareer.Percent}%) sluit aan bij dit soort werk.");
        }

        if (occupation is not null)
        {
            lines.Add($"Op de Nederlandse arbeidsmarkt lijkt dit op {occupation.Title.ToLowerInvariant()} — en dat past bij hoe jij scoort.");
        }

        if (lines.Count == 0)
        {
            lines.Add($"Er zit overlap: je kunt {title.ToLowerInvariant()} serieus overwegen, al is het nog geen kernfit.");
        }

        return lines.Take(4).ToList();
    }

    private static List<string> BuildGaps(
        CareerOccupation? occupation,
        CompetencyScores competencies,
        RiasecScores career)
    {
        var lines = new List<string>();
        foreach (var category in NeededCompetencies(occupation))
        {
            var value = competencies.Get(category);
            if (value < 60)
            {
                lines.Add($"{CompetenceLabel(category)} is nog {value}%. Voor deze functie helpt het als dat omhoog gaat.");
            }
        }

        if (occupation is not null)
        {
            var weak = occupation.Weights
                .OrderByDescending(w => w.Weight)
                .Select(w => (w.Code, Percent: career.Get(w.Code)))
                .FirstOrDefault(x => x.Percent < 55);
            if (weak.Code is not null)
            {
                lines.Add($"De richting {CareerCompassBuilder.TypeLabel(weak.Code).ToLowerInvariant()} scoort {weak.Percent}%. Dat is het gat om te dichten.");
            }
        }

        if (lines.Count == 0)
        {
            lines.Add("Het gat is klein. Het verschil zit vooral in ervaring, diploma of een concreet groeistapje op de werkvloer.");
        }

        return lines.Take(4).ToList();
    }

    private static List<string> BuildSteps(string title, IReadOnlyList<string> gaps, bool fromDeepAnalysis)
    {
        var role = title.ToLowerInvariant();
        var steps = new List<string>
        {
            $"Zoek op de Lobsy-banenkaart in Den Haag en het Westland naar {role} en filter op hoge match.",
            TrainingCopy.GapAdvice,
            gaps.Count > 0
                ? "Pak het grootste gat uit de lijst hierboven: volg een korte training of vraag of je die taak mag oefenen."
                : "Bewaar twee vacatures die voelen als ‘dit is het’ en solliciteer op de beste fit."
        };
        if (!fromDeepAnalysis)
        {
            steps.Add("Wil je een scherper groeistappenplan? Vul de uitgebreide 150-vragen analyse in.");
        }
        else
        {
            steps.Add("Gebruik je loopbaanrapport: kies de taken die het sterkst bij je kernfit horen en laat de rest even liggen.");
        }

        return steps;
    }

    private static string CompetenceLabel(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => "Samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "Afmaken wat je belooft",
        CompetencyTestCatalog.Stressbestendigheid => "Kalm blijven als het druk is",
        CompetencyTestCatalog.Innovatie => "Nieuwe wegen zoeken",
        _ => "Werkstijl"
    };
}
