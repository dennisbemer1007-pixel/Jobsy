using System.Globalization;
using Jobsy.Core.Careers;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public class FutureJobsForYouTests
{
    private static readonly RiasecScores HandsOn = new(82, 40, 28, 74, 36, 61);

    [Fact]
    public void Flag_defaults_off_and_admin_copy_exists()
    {
        Assert.False(FeatureFlagSnapshot.Defaults.FutureJobsForYouEnabled);
        Assert.False(new FeatureFlagSnapshot(false, true).IsEnabled(PlatformFeature.FutureJobsForYou));
        Assert.True(new FeatureFlagSnapshot(false, true, FutureJobsForYouEnabled: true)
            .IsEnabled(PlatformFeature.FutureJobsForYou));

        var entry = PlatformSettingsCatalog.Entries.Single(item => item.Key == "FutureJobsForYouEnabled");
        Assert.False(entry.Read(new PlatformFeatureSnapshot(false, true, "http://localhost", null)) is true);
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("FutureJobs.Title", lang)));
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("FutureJobs.Empty", lang)));
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("FutureJobs.RegionNote", lang)));
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("AdminSettings.FutureJobs.Enabled.Title", lang)));
            Assert.DoesNotContain("Top 10", UiStrings.Get("FutureJobs.Title", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RIASEC", UiStrings.Get("FutureJobs.Lead", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ESCO", UiStrings.Get("FutureJobs.How1", lang), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ITKB", UiStrings.Get("FutureJobs.How2", lang), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Fit_tab_stays_reachable_when_future_jobs_are_on_and_employers_are_off()
    {
        Assert.DoesNotContain(PassportTabs.Fit, PassportTabs.Visible(employersEnabled: false));
        Assert.Contains(PassportTabs.Fit, PassportTabs.Visible(employersEnabled: false, futureJobsEnabled: true));
        Assert.Contains(PassportTabs.Fit, PassportTabs.Visible(employersEnabled: true, futureJobsEnabled: false));

        var passport = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web/Components/Pages/Candidate/Passport.razor"));
        Assert.Contains("_flags.FutureJobsForYouEnabled", passport, StringComparison.Ordinal);
        Assert.Contains("Passport.Fit.Soon", passport, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_when_the_test_is_missing()
    {
        var missing = FutureJobsForYou.Build(null);
        Assert.True(missing.NeedsTest);
        Assert.Empty(missing.Items);

        var partial = FutureJobsForYou.Build(new RiasecScores(80, 40, 20, 70, 30, null));
        Assert.True(partial.NeedsTest);
        Assert.Empty(partial.Items);
    }

    [Fact]
    public void List_uses_only_sourced_strong_demand_and_sorts_fit_then_need()
    {
        var list = FutureJobsForYou.Build(HandsOn);
        Assert.False(list.NeedsTest);
        Assert.InRange(list.Items.Count, 1, FutureJobsForYou.MaxCount);
        Assert.False(string.IsNullOrWhiteSpace(list.Peildatum));
        Assert.Equal(OccupationOutlook.Shared.Peildatum, list.Peildatum);

        var catalog = OccupationCatalog.Shared;
        var outlook = OccupationOutlook.Shared;
        var top = RiasecRanking.Rank(
                HandsOn.Realistic, HandsOn.Investigative, HandsOn.Artistic,
                HandsOn.Social, HandsOn.Enterprising, HandsOn.Conventional)
            .Take(3)
            .Select(item => item.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < list.Items.Count; i++)
        {
            var item = list.Items[i];
            var job = catalog.Get(item.EscoId);
            Assert.NotNull(job);
            Assert.True(job!.IsListable);
            Assert.True(outlook.TryGetSourcedDemand(job.Id, out var demand));
            Assert.NotNull(demand);
            Assert.True(demand!.ItkbRank >= FutureJobsForYou.StrongNeedMinRank);
            Assert.Equal(demand.ItkbRank, item.ItkbRank);
            Assert.Equal(demand.OpeningsPer100, item.OpeningsPer100);
            Assert.Equal(demand.AiLine, item.AiLine);
            Assert.Equal(FutureJobsForYou.DisplayTitle(job.Nl), item.TitleNl);
            Assert.Equal(CareerCompassBuilder.ProfileMatch(job.Oi!, HandsOn), item.FitPercent);
            Assert.All(item.WhyTraitCodes, code => Assert.Contains(code, top));
            Assert.True(FutureJobsForYou.OpeningsMatchStrongNeed(demand.OpeningsTypering));
            Assert.Equal(FutureJobsForYou.NeedLabelNl(item.ItkbRank), UiStrings.Get($"FutureJobs.Need.{item.ItkbRank}", "nl"));

            var why = FutureJobsForYou.WhyNl(item.WhyTraitCodes);
            if (item.WhyTraitCodes.Count > 0)
            {
                Assert.StartsWith("Past bij jou:", why, StringComparison.Ordinal);
                Assert.False(CareerCompassBuilder.ContainsForbiddenJargon(why));
                Assert.DoesNotContain("ITKB", why, StringComparison.OrdinalIgnoreCase);
            }

            if (i == 0)
            {
                continue;
            }

            var previous = list.Items[i - 1];
            var fit = FutureJobsForYou.FitLevel(previous.FitPercent)
                .CompareTo(FutureJobsForYou.FitLevel(item.FitPercent));
            Assert.True(fit >= 0);
            if (fit != 0)
            {
                continue;
            }

            var rank = previous.ItkbRank.CompareTo(item.ItkbRank);
            Assert.True(rank >= 0);
            if (rank == 0)
            {
                Assert.True(previous.OpeningsPer100 >= item.OpeningsPer100);
            }
        }

        var seenGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in list.Items)
        {
            var group = FutureJobsForYou.IscoGroup(catalog.Get(item.EscoId)!.Isco);
            Assert.True(seenGroups.Add(group));
        }

        var weak = catalog.Listable.First(job =>
            outlook.TryGetSourcedDemand(job.Id, out var demand)
            && demand!.ItkbRank < FutureJobsForYou.StrongNeedMinRank);
        Assert.False(FutureJobsForYou.IsEligible(weak, HandsOn));
        Assert.DoesNotContain(list.Items, item => item.EscoId == weak.Id);

        var unsourced = new Occupation
        {
            Id = "00000000-0000-0000-0000-000000000099",
            Nl = "verzonnen beroep",
            Oi = [6, 2, 1, 2, 1, 2],
            Confidence = "high",
            Isco = "9999",
            Brc = "0000"
        };
        Assert.True(unsourced.IsListable);
        Assert.False(outlook.TryGetSourcedDemand(unsourced.Id, out _));
        Assert.False(FutureJobsForYou.IsEligible(unsourced, HandsOn));
        Assert.DoesNotContain(list.Items, item => item.EscoId == unsourced.Id);

        var noProfile = new Occupation
        {
            Id = list.Items[0].EscoId,
            Nl = list.Items[0].TitleNl,
            Oi = [6, 2, 1, 2, 1, 2],
            Confidence = "low"
        };
        Assert.False(noProfile.IsListable);
        Assert.False(FutureJobsForYou.IsEligible(noProfile, HandsOn));
    }

    [Fact]
    public void Order_is_fit_then_need_then_openings_and_stops_at_ten()
    {
        var rows = new List<FutureJobRow>();
        for (var i = 0; i < 12; i++)
        {
            rows.Add(new FutureJobRow($"id-{i}", $"Baan {i:00}", 50 + i, 3, "groot", 10, null, []));
        }

        rows.Add(new FutureJobRow("low-fit", "Aaa", 10, 4, "zeer groot", 99, null, []));
        rows.Add(new FutureJobRow("tie-low-open", "Tie laag", 90, 4, "zeer groot", 20, null, []));
        rows.Add(new FutureJobRow("tie-high-open", "Tie hoog", 90, 4, "zeer groot", 40, null, []));
        rows.Add(new FutureJobRow("tie-weaker-need", "Tie minder", 90, 3, "groot", 80, null, []));
        rows.Add(new FutureJobRow("near-low-fit-high-need", "Band nodig", 60, 4, "zeer groot", 50, null, []));
        rows.Add(new FutureJobRow("near-high-fit-low-need", "Band fit", 64, 3, "groot", 10, null, []));

        var ordered = FutureJobsForYou.Order(rows);
        Assert.Equal(FutureJobsForYou.MaxCount, ordered.Count);
        Assert.Equal("tie-high-open", ordered[0].EscoId);
        Assert.Equal("tie-low-open", ordered[1].EscoId);
        Assert.Equal("tie-weaker-need", ordered[2].EscoId);
        Assert.Equal(FutureJobsForYou.FitLevel(64), FutureJobsForYou.FitLevel(60));
        Assert.Equal("near-low-fit-high-need", ordered[3].EscoId);
        var higherFit = ordered.ToList().FindIndex(row => row.EscoId == "near-high-fit-low-need");
        Assert.True(higherFit > 3);
        Assert.DoesNotContain(ordered, row => row.EscoId == "low-fit");
    }

    [Fact]
    public void Demand_label_follows_itkb_and_low_openings_stay_out()
    {
        Assert.Equal(4, OccupationOutlook.ItkbRank("zeer groot"));
        Assert.Equal("Heel hard nodig", FutureJobsForYou.NeedLabelNl(4));
        Assert.Equal(3, OccupationOutlook.ItkbRank("groot"));
        Assert.Equal("Hard nodig", FutureJobsForYou.NeedLabelNl(3));
        Assert.Equal(2, OccupationOutlook.ItkbRank("enige"));
        Assert.Equal("Een beetje tekort", FutureJobsForYou.NeedLabelNl(2));
        Assert.Null(FutureJobsForYou.NeedLabelNl(-1));

        foreach (var rank in new[] { 0, 1, 2, 3, 4 })
        {
            Assert.Equal(FutureJobsForYou.NeedLabelNl(rank), UiStrings.Get($"FutureJobs.Need.{rank}", "nl"));
        }

        Assert.True(FutureJobsForYou.OpeningsMatchStrongNeed("erg hoog"));
        Assert.True(FutureJobsForYou.OpeningsMatchStrongNeed("hoog"));
        Assert.True(FutureJobsForYou.OpeningsMatchStrongNeed("gemiddeld"));
        Assert.False(FutureJobsForYou.OpeningsMatchStrongNeed("laag"));
        Assert.False(FutureJobsForYou.OpeningsMatchStrongNeed("erg laag"));
        Assert.False(FutureJobsForYou.OpeningsMatchStrongNeed(""));

        var outlook = OccupationOutlook.Shared;
        var low = OccupationCatalog.Shared.Listable.First(job =>
            outlook.TryGetSourcedDemand(job.Id, out var demand)
            && demand!.ItkbRank >= FutureJobsForYou.StrongNeedMinRank
            && !FutureJobsForYou.OpeningsMatchStrongNeed(demand.OpeningsTypering));
        Assert.True(outlook.TryGetSourcedDemand(low.Id, out var lowDemand));
        Assert.True(lowDemand!.OpeningsTypering is "laag" or "erg laag");
        Assert.False(FutureJobsForYou.IsEligible(low, HandsOn));

        var kept = FutureJobsForYou.Build(HandsOn);
        Assert.DoesNotContain(kept.Items, item => item.EscoId == low.Id);
        Assert.All(kept.Items, item =>
        {
            Assert.True(item.ItkbRank >= FutureJobsForYou.StrongNeedMinRank);
            Assert.Equal(item.Typering is "zeer groot" ? "Heel hard nodig" : "Hard nodig", FutureJobsForYou.NeedLabelNl(item.ItkbRank));
        });
    }

    [Fact]
    public void One_occupation_per_isco_group_prefers_the_cbs_name()
    {
        var catalog = OccupationCatalog.Shared;
        var eligible = catalog.Listable.Where(job => FutureJobsForYou.IsEligible(job, HandsOn)).ToList();
        var crowded = eligible
            .GroupBy(job => FutureJobsForYou.IscoGroup(job.Isco))
            .First(group => group.Count() >= 2);
        var chosen = FutureJobsForYou.ChooseRepresentative(crowded.ToList());
        Assert.Contains(crowded, job => job.Id == chosen.Id);

        var cbs = crowded.Where(catalog.IsCbsTitleForItsIsco).ToList();
        if (cbs.Count > 0)
        {
            Assert.Contains(cbs, job => job.Id == chosen.Id);
            var shortest = cbs.Min(job => FutureJobsForYou.DisplayTitle(job.Nl).Length);
            Assert.Equal(shortest, FutureJobsForYou.DisplayTitle(chosen.Nl).Length);
        }

        var list = FutureJobsForYou.Build(HandsOn);
        var groups = list.Items.Select(item => FutureJobsForYou.IscoGroup(catalog.Get(item.EscoId)!.Isco)).ToList();
        Assert.Equal(groups.Count, groups.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Why_line_names_the_overlap_and_collapses_when_it_is_the_same()
    {
        var scores = new RiasecScores(80, 20, 75, 30, 70, 25);
        var artistic = FutureJobsForYou.WhyTraitCodes(scores, [2, 1, 6, 1, 2, 1]);
        var hands = FutureJobsForYou.WhyTraitCodes(scores, [6, 1, 1, 4, 1, 1]);
        Assert.Equal([CareerTestCatalog.Artistic, CareerTestCatalog.Realistic], artistic);
        Assert.Equal([CareerTestCatalog.Realistic], hands);
        Assert.Contains("iets eigens", FutureJobsForYou.WhyNl(artistic), StringComparison.Ordinal);
        Assert.Contains("handen", FutureJobsForYou.WhyNl(hands), StringComparison.Ordinal);
        Assert.DoesNotContain("iets eigens", FutureJobsForYou.WhyNl(hands), StringComparison.Ordinal);

        var same = new FutureJobRow("a", "A", 60, 3, "groot", 20, null, artistic);
        var copy = new FutureJobRow("b", "B", 61, 3, "groot", 20, null, artistic);
        var other = new FutureJobRow("c", "C", 61, 3, "groot", 20, null, hands);
        Assert.True(FutureJobsForYou.SameWhy([same, copy]));
        Assert.False(FutureJobsForYou.SameWhy([same, other]));
        Assert.Equal("", FutureJobsForYou.WhyNl([]));
    }

    [Fact]
    public void Openings_text_uses_the_sourced_number()
    {
        Assert.Equal("50", FutureJobsForYou.FormatOpenings(50));
        Assert.Equal("12.5", FutureJobsForYou.FormatOpenings(12.5));
        Assert.Equal("12,5", FutureJobsForYou.FormatOpenings(12.5, CultureInfo.GetCultureInfo("nl-NL")));
    }

    [Fact]
    public void Ranking_does_not_call_a_model()
    {
        var source = File.ReadAllText(Path.Combine(TestRepo.FindRoot(), "Jobsy.Core/Careers/FutureJobsForYou.cs"));
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mistral", source, StringComparison.OrdinalIgnoreCase);
    }
}
