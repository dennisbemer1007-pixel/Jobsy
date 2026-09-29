using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public class TestsOverviewBuilderTests
{
    [Fact]
    public void Paint_parity_marks_extended_gold_and_counts_done()
    {
        var model = TestsOverviewBuilder.Paint(
            FreeDoneCompetence(),
            null,
            null,
            null,
            new DeepAnalysisState { IsCompleted = true, IsUnlocked = true },
            null,
            null,
            null);

        Assert.Equal(1, model.DoneCount);
        var competence = model.Tiles.Single(t => t.Kind == AssessmentKind.Competence);
        Assert.Equal("gold", competence.Accent);
        Assert.Equal(TestsOverviewBuilder.RowAction.Report, competence.Action);
        Assert.Equal("Test.Status.Extended", competence.StatusKey);
    }

    [Theory]
    [InlineData(false, false, false, false, 0, 25, TestsOverviewBuilder.RowAction.Start)]
    [InlineData(false, false, false, false, 8, 20, TestsOverviewBuilder.RowAction.Continue)]
    [InlineData(true, false, false, false, 25, 25, TestsOverviewBuilder.RowAction.Extended)]
    [InlineData(true, false, true, true, 25, 25, TestsOverviewBuilder.RowAction.Continue)]
    [InlineData(true, true, true, false, 25, 25, TestsOverviewBuilder.RowAction.Report)]
    public void Action_mapping_covers_each_status(
        bool freeDone,
        bool extended,
        bool unlocked,
        bool deepInProgress,
        int answered,
        int total,
        TestsOverviewBuilder.RowAction expected)
    {
        Assert.Equal(
            expected,
            TestsOverviewBuilder.ResolveAction(freeDone, extended, unlocked, deepInProgress, answered, total));
    }

    [Theory]
    [InlineData(3, "TestResult.Quota.Line")]
    [InlineData(2, "TestResult.Quota.Line")]
    [InlineData(1, "TestResult.Quota.LineOne")]
    [InlineData(0, "TestResult.Quota.Zero")]
    public void Quota_key_uses_testresultaten_strings(int remaining, string key)
        => Assert.Equal(key, TestsOverviewBuilder.QuotaKey(remaining));

    [Fact]
    public void Lowest_competence_picks_minimum_workplace_score()
    {
        var lowest = TestsOverviewBuilder.LowestCompetence(new CompetencyScoreSet
        {
            Samenwerken = 80,
            Resultaatgerichtheid = 35,
            Stressbestendigheid = 60,
            Innovatie = 70
        });
        Assert.NotNull(lowest);
        Assert.Equal(CompetencyTestCatalog.Resultaatgerichtheid, lowest!.Value.Category);
        Assert.Equal(35, lowest.Value.Score);
    }

    [Fact]
    public void Progress_segments_lock_report_outline_after_quick_scan()
    {
        var tile = TestsOverviewBuilder.Paint(
            FreeDoneCompetence(),
            null, null, null,
            null, null, null, null).Tiles[0];
        var (_, _, report, locked) = TestsOverviewBuilder.ProgressSegments(tile);
        Assert.Equal(0, report);
        Assert.True(locked);
    }

    private static CandidateCompetencyState FreeDoneCompetence()
        => new()
        {
            Status = "Completed",
            AnsweredCount = 25,
            QuestionCount = 25,
            Scores = new CompetencyScoreSet
            {
                Samenwerken = 70,
                Resultaatgerichtheid = 40,
                Stressbestendigheid = 55,
                Innovatie = 60,
                Extraversie = 50
            },
            CompletedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
}

public class CourseSlotRulesTests
{
    [Fact]
    public void Free_first_then_partner_max_two()
    {
        var offers = new[]
        {
            Make("paid", isFree: false, isPartner: false, show: true, keys: "plannen", scoreBoost: true),
            Make("free-b", isFree: true, isPartner: false, show: true, keys: "plannen", sort: 2),
            Make("free-a", isFree: true, isPartner: false, show: true, keys: "plannen", sort: 1),
            Make("partner", isFree: false, isPartner: true, show: true, keys: "plannen", affiliate: "ZORG1"),
        };

        var slots = CourseSlotRules.Pick(offers, Ctx("plannen organiseren"));
        Assert.Equal(2, slots.Count);
        Assert.True(slots[0].IsFree);
        Assert.Equal("free-a", slots[0].Offer.Title);
        Assert.False(slots[1].IsFree);
        Assert.Equal("partner", slots[1].Offer.Title);
    }

    [Fact]
    public void No_free_match_returns_empty()
    {
        var offers = new[]
        {
            Make("partner-only", isFree: false, isPartner: true, show: true, keys: "plannen", affiliate: "X")
        };
        Assert.Empty(CourseSlotRules.Pick(offers, Ctx("plannen")));
    }

    [Fact]
    public void Partner_without_affiliate_excluded()
    {
        var offers = new[]
        {
            Make("free", isFree: true, isPartner: false, show: true, keys: "plannen"),
            Make("bad-partner", isFree: false, isPartner: true, show: true, keys: "plannen", affiliate: null)
        };
        var slots = CourseSlotRules.Pick(offers, Ctx("plannen"));
        Assert.Single(slots);
        Assert.True(slots[0].IsFree);
    }

    [Fact]
    public void Paid_non_partner_excluded_even_when_shown()
    {
        var offers = new[]
        {
            Make("free", isFree: true, isPartner: false, show: true, keys: "plannen"),
            Make("paid", isFree: false, isPartner: false, show: true, keys: "plannen")
        };
        var slots = CourseSlotRules.Pick(offers, Ctx("plannen"));
        Assert.Single(slots);
        Assert.Equal("free", slots[0].Offer.Title);
    }

    [Fact]
    public void Both_flags_true_is_invalid()
    {
        Assert.NotNull(CourseSlotRules.ValidateFlags(true, true, "A", "https://example.com/cursus/plannen"));
        var offer = Make("bad", isFree: true, isPartner: true, show: true, keys: "plannen", affiliate: "A");
        Assert.False(CourseSlotRules.IsStructurallyValid(offer));
    }

    [Fact]
    public void Hidden_from_passport_never_picked()
    {
        var offers = new[]
        {
            Make("free-hidden", isFree: true, isPartner: false, show: false, keys: "plannen")
        };
        Assert.Empty(CourseSlotRules.Pick(offers, Ctx("plannen")));
    }

    private static CourseSlotRules.Context Ctx(string blob)
        => new(TrainingFieldCatalog.Detect([blob]), blob);

    private static TrainingOffer Make(
        string title,
        bool isFree,
        bool isPartner,
        bool show,
        string keys,
        string? affiliate = "CODE",
        int sort = 1,
        bool scoreBoost = false)
    {
        var provider = new TrainingProvider
        {
            Id = Guid.NewGuid(),
            Name = "LeerPlein",
            Kind = TrainingProviderKind.RegionalPartner,
            Network = TrainingNetwork.Direct,
            BaseUrl = "https://leerplein.example/",
            FieldsCsv = "vaardigheden",
            Region = "Den Haag",
            IsActive = true
        };
        return new TrainingOffer
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Provider = provider,
            Title = title,
            FieldsCsv = "vaardigheden",
            KeysCsv = keys,
            ExternalPath = "/cursussen/" + title.ToLowerInvariant(),
            IsActive = true,
            SortOrder = sort,
            IsFree = isFree,
            IsPartner = isPartner,
            AffiliateCode = affiliate,
            ShowInPassport = show,
            Type = TrainingOfferType.Workshop,
            DurationValue = 2,
            DurationUnit = TrainingDurationUnit.Hours,
            Delivery = TrainingDeliveryMode.Online
        };
    }
}

public class TrainingTrackingAffiliateTests
{
    [Fact]
    public void AppendParameters_includes_optional_affiliate_code()
    {
        var url = TrainingTracking.AppendParameters(
            "https://www.example.com/cursus/plannen",
            "abc123abc123abcd",
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            TrainingTracking.CampaignCompetence,
            "affiliate",
            "ZORG42");
        Assert.Contains("aff=ZORG42", url, StringComparison.Ordinal);
        Assert.Equal("sponsored noopener noreferrer", TrainingTracking.RelFor(true));
        Assert.Equal("noopener", TrainingTracking.RelFor(false));
    }
}
