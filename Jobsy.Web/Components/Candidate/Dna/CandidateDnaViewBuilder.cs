using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Shared.Charts;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Dna;

/// <summary>
/// Pure builder for Mijn DNA cards / highlights / slides / details.
/// Shared by <c>DnaPanel</c> and the passport Mijn DNA tab.
/// </summary>
public static class CandidateDnaViewBuilder
{
    public sealed record DnaHighlight(string LabelKey, string Value);

    public sealed record DnaSlide(
        AssessmentKind Kind,
        string TitleKey,
        bool Completed,
        bool Extended,
        bool IsProvisional,
        bool IsGhost,
        string StartHref,
        int QuestionCount,
        IReadOnlyList<ChartAxisValue> Axes,
        IReadOnlyList<ChartAxisValue> Bars,
        IReadOnlyList<(string LowLabel, string HighLabel, int? Percent)> Poles,
        string ChartAltKey,
        string TeaserKey,
        string AriaName);

    public sealed record DetailBlock(
        IReadOnlyList<(string LowLabel, string HighLabel, int? Percent)> Poles,
        IReadOnlyList<ChartAxisValue> Bars,
        bool IsGhost,
        string StartHref,
        int QuestionCount,
        string Aside);

    public sealed record DnaCard(
        AssessmentKind Kind,
        string TitleKey,
        bool Completed,
        bool Extended,
        string DetailHref,
        string StartHref,
        int Answered,
        int QuestionCount,
        int MinutesApprox,
        bool IsProvisional,
        string? TopLabel,
        string Summary,
        IReadOnlyList<ChartAxisValue> Axes,
        IReadOnlyList<ChartAxisValue> Bars,
        IReadOnlyList<(string LowLabel, string HighLabel, int? Percent)> Poles)
    {
        public int Remaining => Math.Max(0, QuestionCount - Answered);
    }

    public sealed record DnaViewModel(
        IReadOnlyList<DnaCard> Cards,
        IReadOnlyList<DnaHighlight> Highlights,
        IReadOnlyList<DnaSlide> Slides,
        DetailBlock? CultureDetail,
        DetailBlock? ValuesDetail,
        int CompletedCount);

    public delegate string TextLookup(string key);
    public delegate string TextFormat(string key, params object[] args);

    public static int EffectiveCompletenessPercent(
        CandidateKompasState? kompas,
        CandidateDnaSummary? dna,
        int fallback = 0)
        => kompas?.ProfileCompletenessPercent
           ?? dna?.ProfileCompletenessPercent
           ?? fallback;

    public static DnaViewModel FromKompas(CandidateKompasState kompas, TextLookup t, TextFormat format)
        => Paint(
            kompas.Competencies?.Status,
            kompas.Competencies?.Scores ?? kompas.Competencies?.PreviewScores,
            kompas.Competencies?.AnsweredCount ?? 0,
            kompas.CareerInterests?.Status,
            kompas.CareerInterests?.Scores ?? kompas.CareerInterests?.PreviewScores,
            kompas.CareerInterests?.AnsweredCount ?? 0,
            kompas.Culture?.Status,
            kompas.Culture?.Scores ?? kompas.Culture?.PreviewScores,
            kompas.Culture?.AnsweredCount ?? 0,
            kompas.Values?.Status,
            kompas.Values?.Scores ?? kompas.Values?.PreviewScores,
            kompas.Values?.AnsweredCount ?? 0,
            kompas.CompetenceDeep,
            kompas.CareerDeep,
            kompas.CultureDeep,
            kompas.ValuesDeep,
            t,
            format);

    public static DnaViewModel FromDnaSummary(CandidateDnaSummary dna, TextLookup t, TextFormat format)
    {
        var competence = dna.Competencies;
        var career = dna.CareerInterests;
        var culture = dna.Culture;
        var values = dna.Values;
        return Paint(
            competence?.Status,
            competence?.Scores ?? competence?.PreviewScores,
            competence?.AnsweredCount ?? 0,
            career?.Status,
            career?.Scores ?? career?.PreviewScores,
            career?.AnsweredCount ?? 0,
            culture?.Status,
            culture?.Scores ?? culture?.PreviewScores,
            culture?.AnsweredCount ?? 0,
            values?.Status,
            values?.Scores ?? values?.PreviewScores,
            values?.AnsweredCount ?? 0,
            DeepStub(competence?.DeepCompleted ?? false),
            DeepStub(career?.DeepCompleted ?? false),
            DeepStub(culture?.DeepCompleted ?? false),
            DeepStub(values?.DeepCompleted ?? false),
            t,
            format);
    }

    private static DeepAnalysisState? DeepStub(bool completed)
        => completed ? new DeepAnalysisState { IsCompleted = true } : null;

    public static DnaViewModel Paint(
        string? competenceStatus,
        CompetencyScoreSet? competenceScores,
        int competenceAnswered,
        string? careerStatus,
        RiasecScoreSet? careerScores,
        int careerAnswered,
        string? cultureStatus,
        CulturePersonalityScoreSet? cultureScores,
        int cultureAnswered,
        string? valuesStatus,
        SchwartzValuesScoreSet? valuesScores,
        int valuesAnswered,
        DeepAnalysisState? competenceDeep,
        DeepAnalysisState? careerDeep,
        DeepAnalysisState? cultureDeep,
        DeepAnalysisState? valuesDeep,
        TextLookup t,
        TextFormat format)
    {
        var competence = BuildCompetence(competenceStatus, competenceScores, competenceAnswered, competenceDeep, t, format);
        var career = BuildCareer(careerStatus, careerScores, careerAnswered, careerDeep, t, format);
        var culture = BuildCulture(cultureStatus, cultureScores, cultureAnswered, cultureDeep, t);
        var values = BuildValues(valuesStatus, valuesScores, valuesAnswered, valuesDeep, t);

        var cards = new List<DnaCard> { competence, career, culture, values };
        var highlights = new List<DnaHighlight>();
        if (competence.TopLabel is { } strong)
        {
            highlights.Add(new DnaHighlight("Dna.HighlightStrongest", strong));
        }

        if (career.TopLabel is { } work)
        {
            highlights.Add(new DnaHighlight("Dna.HighlightWork", work));
        }

        if (values.TopLabel is { } important)
        {
            highlights.Add(new DnaHighlight("Dna.HighlightImportant", important));
        }

        var slides = cards.Select(c => BuildSlide(c, t, format)).ToList();
        return new DnaViewModel(
            cards,
            highlights,
            slides,
            BuildCultureDetail(culture, t),
            BuildValuesDetail(values, t),
            cards.Count(c => c.Completed));
    }

    private static DnaSlide BuildSlide(DnaCard card, TextLookup t, TextFormat format)
    {
        var ghost = !card.Completed && !card.IsProvisional;
        return new DnaSlide(
            card.Kind,
            card.TitleKey,
            card.Completed,
            card.Extended,
            card.IsProvisional,
            ghost,
            card.StartHref,
            card.QuestionCount,
            card.Axes,
            card.Bars,
            card.Poles,
            ChartAltKey(card.Kind),
            TeaserKey(card.Kind),
            format(
                "Dna.TileA11y",
                t(card.TitleKey),
                ghost
                    ? t("Test.Status.NotDone")
                    : card.IsProvisional
                        ? t("Onboarding.ProvisionalBadge")
                        : t("Dna.StatusComplete"),
                card.Answered,
                card.QuestionCount));
    }

    private static DetailBlock BuildCultureDetail(DnaCard culture, TextLookup t)
    {
        var aside = culture.Completed
            ? t("Dna.StatusComplete")
            : culture.IsProvisional
                ? t("Onboarding.ProvisionalBadge")
                : t("Test.Status.NotDone");
        return new DetailBlock(
            culture.Poles.Count > 0 ? culture.Poles : GhostCulturePoles(t),
            [],
            !culture.Completed && !culture.IsProvisional,
            culture.StartHref,
            culture.QuestionCount,
            aside);
    }

    private static DetailBlock BuildValuesDetail(DnaCard values, TextLookup t)
    {
        var aside = values.Completed
            ? t("Dna.StatusComplete")
            : values.IsProvisional
                ? t("Onboarding.ProvisionalBadge")
                : t("Test.Status.NotDone");
        return new DetailBlock(
            [],
            values.Bars.Count > 0 ? values.Bars : GhostValueBars(),
            !values.Completed && !values.IsProvisional,
            values.StartHref,
            values.QuestionCount,
            aside);
    }

    private static List<(string LowLabel, string HighLabel, int? Percent)> GhostCulturePoles(TextLookup t)
        => CulturePersonalityCatalog.CultureDimensionCodes
            .Select(code =>
            {
                var (low, high) = DnaSummarySentences.CulturePoles(code);
                return (t(low), t(high), (int?)null);
            })
            .ToList();

    private static List<ChartAxisValue> GhostValueBars()
        => SchwartzValuesCatalog.CategoryCodes
            .Select(code => new ChartAxisValue(DimensionLabels.For(code), null))
            .ToList();

    private static string ChartAltKey(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => "Dna.ChartAlt.Competence",
        AssessmentKind.Career => "Dna.ChartAlt.Career",
        AssessmentKind.Culture => "Dna.ChartAlt.Culture",
        _ => "Dna.ChartAlt.Values"
    };

    private static string TeaserKey(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => "Dna.TeaserCompetence",
        AssessmentKind.Career => "Dna.TeaserCareer",
        AssessmentKind.Culture => "Dna.TeaserCulture",
        _ => "Dna.TeaserValues"
    };

    private static DnaCard BuildCompetence(
        string? status, CompetencyScoreSet? scores, int answered, DeepAnalysisState? deep,
        TextLookup t, TextFormat format)
    {
        var def = AssessmentTestCatalog.TryGet(AssessmentKind.Competence)!;
        var freeDone = IsCompleted(status);
        var extended = deep is { IsCompleted: true };
        var completed = freeDone || extended;
        var provisional = !completed && answered > 0;
        var axes = new List<ChartAxisValue>();
        string? top = null;
        string summary = t("Dna.TileNotStarted");

        if ((completed || provisional) && scores is not null)
        {
            var ranked = Rank(
                (CompetencyTestCatalog.Samenwerken, scores.Samenwerken),
                (CompetencyTestCatalog.Resultaatgerichtheid, scores.Resultaatgerichtheid),
                (CompetencyTestCatalog.Stressbestendigheid, scores.Stressbestendigheid),
                (CompetencyTestCatalog.Innovatie, scores.Innovatie),
                (CompetencyTestCatalog.Extraversie, scores.Extraversie));
            foreach (var code in new[]
                     {
                         CompetencyTestCatalog.Samenwerken,
                         CompetencyTestCatalog.Resultaatgerichtheid,
                         CompetencyTestCatalog.Stressbestendigheid,
                         CompetencyTestCatalog.Innovatie,
                         CompetencyTestCatalog.Extraversie
                     })
            {
                var pct = ranked.FirstOrDefault(r => r.Code == code).Percent;
                var has = ranked.Any(r => r.Code == code);
                axes.Add(new ChartAxisValue(DeepAnalysisQuestionHelp.DomainLabel(code), has ? pct : null));
            }

            if (ranked.Count > 0)
            {
                top = DeepAnalysisQuestionHelp.DomainLabel(ranked[0].Code);
                summary = format("Dna.TileSummaryStrong", top);
            }
        }

        return new DnaCard(
            AssessmentKind.Competence,
            def.TitleKey,
            completed,
            extended,
            def.DetailHref,
            def.FreeStartHref,
            answered,
            def.FreeQuestionCount,
            def.FreeMinutesApprox,
            provisional,
            top,
            summary,
            axes,
            [],
            []);
    }

    private static DnaCard BuildCareer(
        string? status, RiasecScoreSet? scores, int answered, DeepAnalysisState? deep,
        TextLookup t, TextFormat format)
    {
        var def = AssessmentTestCatalog.TryGet(AssessmentKind.Career)!;
        var freeDone = IsCompleted(status);
        var extended = deep is { IsCompleted: true };
        var completed = freeDone || extended;
        var provisional = !completed && answered > 0;
        var axes = new List<ChartAxisValue>();
        var bars = new List<ChartAxisValue>();
        string? top = null;
        string summary = t("Dna.TileNotStarted");

        if ((completed || provisional) && scores is not null)
        {
            var ranked = RiasecRanking.Rank(
                    scores.Realistic, scores.Investigative, scores.Artistic,
                    scores.Social, scores.Enterprising, scores.Conventional)
                .Select(x => (Code: x.Code, Percent: x.Value))
                .ToList();
            foreach (var code in new[]
                     {
                         CareerTestCatalog.Realistic,
                         CareerTestCatalog.Investigative,
                         CareerTestCatalog.Artistic,
                         CareerTestCatalog.Social,
                         CareerTestCatalog.Enterprising,
                         CareerTestCatalog.Conventional
                     })
            {
                var hit = ranked.FirstOrDefault(r => r.Code == code);
                var has = ranked.Any(r => r.Code == code);
                axes.Add(new ChartAxisValue(CareerCompassBuilder.TypeLabel(code), has ? hit.Percent : null, code[..1]));
            }

            foreach (var (code, pct) in ranked.Take(3))
            {
                bars.Add(new ChartAxisValue(CareerCompassBuilder.TypeLabel(code), pct, code[..1]));
            }

            if (ranked.Count > 0)
            {
                top = CareerCompassBuilder.TypeLabel(ranked[0].Code);
                summary = format(
                    "Dna.TileSummaryTop",
                    string.Join(" · ", ranked.Take(3).Select(r => CareerCompassBuilder.TypeLabel(r.Code))));
            }
        }

        return new DnaCard(
            AssessmentKind.Career,
            def.TitleKey,
            completed,
            extended,
            def.DetailHref,
            def.FreeStartHref,
            answered,
            def.FreeQuestionCount,
            def.FreeMinutesApprox,
            provisional,
            top,
            summary,
            axes,
            bars,
            []);
    }

    private static DnaCard BuildCulture(
        string? status, CulturePersonalityScoreSet? scores, int answered, DeepAnalysisState? deep,
        TextLookup t)
    {
        var def = AssessmentTestCatalog.TryGet(AssessmentKind.Culture)!;
        var freeDone = IsCompleted(status);
        var extended = deep is { IsCompleted: true };
        var completed = freeDone || extended;
        var provisional = !completed && answered > 0;
        var poles = new List<(string LowLabel, string HighLabel, int? Percent)>();
        string? top = null;
        string summary = t("Dna.TileNotStarted");

        if ((completed || provisional) && scores is not null)
        {
            var pairs = new (string Code, int? Value)[]
            {
                (CulturePersonalityCatalog.Autonomy, scores.Autonomy),
                (CulturePersonalityCatalog.Informal, scores.Informal),
                (CulturePersonalityCatalog.Collaboration, scores.Collaboration),
                (CulturePersonalityCatalog.Flexibility, scores.Flexibility),
                (CulturePersonalityCatalog.Innovation, scores.Innovation),
                (CulturePersonalityCatalog.PeopleFirst, scores.PeopleFirst)
            };
            foreach (var (code, value) in pairs)
            {
                var (low, high) = DnaSummarySentences.CulturePoles(code);
                poles.Add((t(low), t(high), value));
            }

            var ranked = DimensionRanking.Rank(pairs, DimensionRanking.CultureTieBreak)
                .Select(x => (Code: x.Code, Percent: x.Score))
                .ToList();
            if (ranked.Count > 0)
            {
                top = t(DnaSummarySentences.CulturePoles(ranked[0].Code).HighPoleKey);
                summary = string.Join(
                    " · ",
                    ranked.Take(2).Select(r => t(DnaSummarySentences.CulturePoles(r.Code).HighPoleKey)));
            }
        }
        else
        {
            poles = GhostCulturePoles(t);
        }

        return new DnaCard(
            AssessmentKind.Culture,
            def.TitleKey,
            completed,
            extended,
            def.DetailHref,
            def.FreeStartHref,
            answered,
            def.FreeQuestionCount,
            def.FreeMinutesApprox,
            provisional,
            top,
            summary,
            [],
            [],
            poles);
    }

    private static DnaCard BuildValues(
        string? status, SchwartzValuesScoreSet? scores, int answered, DeepAnalysisState? deep,
        TextLookup t)
    {
        var def = AssessmentTestCatalog.TryGet(AssessmentKind.Values)!;
        var freeDone = IsCompleted(status);
        var extended = deep is { IsCompleted: true };
        var completed = freeDone || extended;
        var provisional = !completed && answered > 0;
        var bars = new List<ChartAxisValue>();
        string? top = null;
        string summary = t("Dna.TileNotStarted");

        if ((completed || provisional) && scores is not null)
        {
            var ranked = DimensionRanking.Rank(
                [
                    (SchwartzValuesCatalog.Autonomy, scores.Autonomy),
                    (SchwartzValuesCatalog.Connection, scores.Connection),
                    (SchwartzValuesCatalog.Achievement, scores.Achievement),
                    (SchwartzValuesCatalog.Stability, scores.Stability),
                    (SchwartzValuesCatalog.Impact, scores.Impact)
                ],
                DimensionRanking.ValueTieBreak)
                .Select(x => (Code: x.Code, Percent: x.Score))
                .ToList();
            bars = ranked
                .Select(r => new ChartAxisValue(DimensionLabels.For(r.Code), r.Percent))
                .Concat(SchwartzValuesCatalog.CategoryCodes
                    .Where(c => ranked.All(r => r.Code != c))
                    .Select(c => new ChartAxisValue(DimensionLabels.For(c), null)))
                .ToList();

            if (ranked.Count > 0)
            {
                top = DimensionLabels.For(ranked[0].Code);
                summary = top;
            }
        }
        else
        {
            bars = GhostValueBars();
        }

        return new DnaCard(
            AssessmentKind.Values,
            def.TitleKey,
            completed,
            extended,
            def.DetailHref,
            def.FreeStartHref,
            answered,
            def.FreeQuestionCount,
            def.FreeMinutesApprox,
            provisional,
            top,
            summary,
            [],
            bars,
            []);
    }

    private static List<(string Code, int Percent)> Rank(params (string Code, int? Value)[] items)
        => items
            .Where(x => x.Value is not null)
            .Select(x => (x.Code, Percent: x.Value!.Value))
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();

    private static bool IsCompleted(string? status)
        => string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);

    /// <summary>Nearest unfinished test remaining questions (0 when all done).</summary>
    public static int NearestUnfinishedRemaining(IReadOnlyList<DnaCard> cards)
    {
        var unfinished = cards
            .Where(c => !c.Completed)
            .OrderBy(c => c.Remaining)
            .ThenBy(c => c.Kind)
            .FirstOrDefault();
        return unfinished?.Remaining ?? 0;
    }

    /// <summary>Arc fill 0–1 for the DNA ring from a card.</summary>
    public static double ArcProgress(DnaCard card)
    {
        if (card.Completed)
        {
            return 1d;
        }

        if (card.QuestionCount <= 0)
        {
            return 0d;
        }

        return Math.Clamp(card.Answered / (double)card.QuestionCount, 0d, 1d);
    }
}
