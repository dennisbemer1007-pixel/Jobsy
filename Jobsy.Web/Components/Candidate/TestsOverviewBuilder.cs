using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate;

/// <summary>
/// Pure builder for the classic Tests overview tiles and passport Mijn tests rows.
/// Shared by <see cref="TestsOverviewPanel"/> and <c>PassportTestsTab</c>.
/// </summary>
public static class TestsOverviewBuilder
{
    public enum RowAction
    {
        Start,
        Continue,
        Extended,
        Report
    }

    public sealed record OverviewTile(
        AssessmentKind Kind,
        string TitleKey,
        string Href,
        string Accent,
        string StatusKey,
        string Icon,
        string? Outcome,
        int FreeCount,
        int FreeMinutes,
        bool FreeDone,
        bool ExtendedDone,
        bool DeepUnlocked,
        bool DeepInProgress,
        int FreeAnswered,
        int FreeTotal,
        int DeepAnswered,
        int DeepTotal,
        DateTime? LastActivityUtc,
        RowAction Action,
        string QuestionKey)
    {
        public bool FreeInProgress => !FreeDone && FreeAnswered > 0 && FreeAnswered < FreeTotal;
        public bool ReportReady => ExtendedDone;
        public bool QuickDoneReportLocked => FreeDone && !ExtendedDone;
    }

    public sealed record OverviewModel(
        IReadOnlyList<OverviewTile> Tiles,
        int DoneCount,
        AssessmentKind? HighlightKind,
        AssessmentKind? LockedPreviewKind)
    {
        public bool AllExtendedDone => Tiles.Count > 0 && Tiles.All(t => t.ExtendedDone);
    }

    public static OverviewModel FromKompas(CandidateKompasState kompas)
        => Paint(
            kompas.Competencies,
            kompas.CareerInterests,
            kompas.Culture,
            kompas.Values,
            kompas.CompetenceDeep,
            kompas.CareerDeep,
            kompas.CultureDeep,
            kompas.ValuesDeep);

    public static OverviewModel Paint(
        CandidateCompetencyState? competence,
        CandidateCareerInterestState? career,
        CandidateCultureState? culture,
        CandidateValuesState? values,
        DeepAnalysisState? competenceDeep,
        DeepAnalysisState? careerDeep,
        DeepAnalysisState? cultureDeep,
        DeepAnalysisState? valuesDeep)
    {
        var tiles = new List<OverviewTile>(4);
        foreach (var def in AssessmentTestCatalog.All)
        {
            var (freeDone, outcome, freeAnswered, freeTotal, freeUpdated) = def.Kind switch
            {
                AssessmentKind.Competence => (
                    IsCompleted(competence?.Status),
                    AssessmentOutcomeLines.Competence(
                        competence?.Scores?.Samenwerken,
                        competence?.Scores?.Resultaatgerichtheid,
                        competence?.Scores?.Stressbestendigheid,
                        competence?.Scores?.Innovatie,
                        competence?.Scores?.Extraversie),
                    competence?.AnsweredCount ?? 0,
                    competence?.QuestionCount > 0 ? competence.QuestionCount : def.FreeQuestionCount,
                    competence?.UpdatedAtUtc ?? competence?.CompletedAtUtc),
                AssessmentKind.Career => (
                    IsCompleted(career?.Status),
                    AssessmentOutcomeLines.Career(
                        career?.Scores?.Realistic,
                        career?.Scores?.Investigative,
                        career?.Scores?.Artistic,
                        career?.Scores?.Social,
                        career?.Scores?.Enterprising,
                        career?.Scores?.Conventional),
                    career?.AnsweredCount ?? 0,
                    career?.QuestionCount > 0 ? career.QuestionCount : def.FreeQuestionCount,
                    career?.UpdatedAtUtc ?? career?.CompletedAtUtc),
                AssessmentKind.Culture => (
                    IsCompleted(culture?.Status),
                    AssessmentOutcomeLines.Culture(
                        culture?.Scores?.Autonomy,
                        culture?.Scores?.Informal,
                        culture?.Scores?.Collaboration,
                        culture?.Scores?.Flexibility,
                        culture?.Scores?.Innovation,
                        culture?.Scores?.PeopleFirst),
                    culture?.AnsweredCount ?? 0,
                    culture?.QuestionCount > 0 ? culture.QuestionCount : def.FreeQuestionCount,
                    culture?.CompletedAtUtc),
                _ => (
                    IsCompleted(values?.Status),
                    AssessmentOutcomeLines.Values(
                        values?.Scores?.Autonomy,
                        values?.Scores?.Connection,
                        values?.Scores?.Achievement,
                        values?.Scores?.Stability,
                        values?.Scores?.Impact),
                    values?.AnsweredCount ?? 0,
                    values?.QuestionCount > 0 ? values.QuestionCount : def.FreeQuestionCount,
                    values?.CompletedAtUtc)
            };

            var deep = def.Kind switch
            {
                AssessmentKind.Competence => competenceDeep,
                AssessmentKind.Career => careerDeep,
                AssessmentKind.Culture => cultureDeep,
                _ => valuesDeep
            };

            var extended = deep is { IsCompleted: true };
            var deepUnlocked = deep?.IsUnlocked == true;
            var deepAnswered = deep?.AnsweredCount ?? 0;
            var deepTotal = deep?.QuestionCount > 0 ? deep.QuestionCount : def.DeepQuestionCount;
            var deepInProgress = deepUnlocked && !extended && deepAnswered > 0;
            var lastActivity = MaxUtc(
                freeUpdated,
                deep?.CompletedAtUtc,
                deep?.UnlockedAtUtc,
                deep?.ReportGeneratedAtUtc);

            string accent;
            string statusKey;
            string icon;
            if (extended)
            {
                accent = "gold";
                statusKey = "Test.Status.Extended";
                icon = "★";
            }
            else if (freeDone)
            {
                accent = "success";
                statusKey = "Test.Status.FreeDone";
                icon = "✓";
            }
            else
            {
                accent = "pending";
                statusKey = "Test.Status.NotDone";
                icon = "○";
            }

            var action = ResolveAction(freeDone, extended, deepUnlocked, deepInProgress, freeAnswered, freeTotal);
            tiles.Add(new OverviewTile(
                def.Kind,
                def.TitleKey,
                def.DetailHref,
                accent,
                statusKey,
                icon,
                freeDone || extended ? outcome : null,
                def.FreeQuestionCount,
                def.FreeMinutesApprox,
                freeDone,
                extended,
                deepUnlocked,
                deepInProgress,
                freeAnswered,
                freeTotal,
                deepAnswered,
                deepTotal,
                lastActivity,
                action,
                QuestionKeyFor(def.Kind)));
        }

        var doneCount = tiles.Count(t => t.Accent is "success" or "gold");
        var highlight = tiles
            .Where(t => t.LastActivityUtc is not null)
            .OrderByDescending(t => t.LastActivityUtc)
            .Select(t => (AssessmentKind?)t.Kind)
            .FirstOrDefault()
            ?? tiles.FirstOrDefault(t => t.Action is RowAction.Continue or RowAction.Extended or RowAction.Start)?.Kind;

        var lockedPreview = tiles.FirstOrDefault(t => !t.ExtendedDone && t.Action is RowAction.Continue or RowAction.Extended)?.Kind
                            ?? tiles.FirstOrDefault(t => !t.ExtendedDone)?.Kind;

        return new OverviewModel(tiles, doneCount, highlight, lockedPreview);
    }

    public static RowAction ResolveAction(
        bool freeDone,
        bool extendedDone,
        bool deepUnlocked,
        bool deepInProgress,
        int freeAnswered,
        int freeTotal)
    {
        if (extendedDone)
        {
            return RowAction.Report;
        }

        if (deepInProgress || deepUnlocked)
        {
            return RowAction.Continue;
        }

        if (!freeDone && freeAnswered > 0 && freeAnswered < freeTotal)
        {
            return RowAction.Continue;
        }

        if (freeDone)
        {
            return RowAction.Extended;
        }

        return RowAction.Start;
    }

    public static string ActionLabelKey(RowAction action) => action switch
    {
        RowAction.Report => "Passport.Tests.Action.Report",
        RowAction.Continue => "Passport.Tests.Action.Continue",
        RowAction.Extended => "Passport.Tests.Action.Extended",
        _ => "Passport.Tests.Action.Start"
    };

    public static string ActionHref(OverviewTile tile)
    {
        var def = AssessmentTestCatalog.TryGet(tile.Kind)!;
        return tile.Action switch
        {
            RowAction.Report => tile.Href,
            RowAction.Extended => def.DeepStartHref,
            RowAction.Continue when tile.DeepInProgress || tile.DeepUnlocked
                => def.DeepStartHref,
            RowAction.Continue => def.FreeStartHref,
            _ => def.FreeStartHref
        };
    }

    /// <summary>Segment fill 0..1 for Quick-Scan / Uitgebreid / Rapport.</summary>
    public static (double Quick, double Extended, double Report, bool ReportLockedOutline) ProgressSegments(OverviewTile tile)
    {
        var quick = tile.FreeDone
            ? 1.0
            : tile.FreeTotal <= 0
                ? 0
                : Math.Clamp(tile.FreeAnswered / (double)tile.FreeTotal, 0, 1);
        var extended = tile.ExtendedDone
            ? 1.0
            : tile.DeepInProgress || tile.DeepUnlocked
                ? (tile.DeepTotal <= 0 ? 0.35 : Math.Clamp(tile.DeepAnswered / (double)tile.DeepTotal, 0.15, 0.95))
                : tile.FreeDone ? 0.0 : 0.0;
        var report = tile.ExtendedDone ? 1.0 : 0.0;
        var lockedOutline = tile.FreeDone && !tile.ExtendedDone;
        return (quick, extended, report, lockedOutline);
    }

    public static string QuotaKey(int remaining) => remaining switch
    {
        <= 0 => "TestResult.Quota.Zero",
        1 => "TestResult.Quota.LineOne",
        _ => "TestResult.Quota.Line"
    };

    /// <summary>Lowest workplace competence for Groei verder matching.</summary>
    public static (string Category, int Score)? LowestCompetence(CompetencyScoreSet? scores)
    {
        if (scores is null)
        {
            return null;
        }

        (string Cat, int? Val)[] pairs =
        [
            (CompetencyTestCatalog.Samenwerken, scores.Samenwerken),
            (CompetencyTestCatalog.Resultaatgerichtheid, scores.Resultaatgerichtheid),
            (CompetencyTestCatalog.Stressbestendigheid, scores.Stressbestendigheid),
            (CompetencyTestCatalog.Innovatie, scores.Innovatie)
        ];

        var ranked = pairs.Where(p => p.Val is int).Select(p => (p.Cat, Score: p.Val!.Value)).ToList();
        if (ranked.Count == 0)
        {
            return null;
        }

        var lowest = ranked.OrderBy(p => p.Score).First();
        return (lowest.Cat, lowest.Score);
    }

    private static string QuestionKeyFor(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => "Passport.Tests.Q.Competence",
        AssessmentKind.Career => "Passport.Tests.Q.Career",
        AssessmentKind.Culture => "Passport.Tests.Q.Culture",
        _ => "Passport.Tests.Q.Values"
    };

    private static bool IsCompleted(string? status)
        => string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);

    private static DateTime? MaxUtc(params DateTime?[] values)
        => values.Where(v => v is not null).DefaultIfEmpty(null).Max();
}
