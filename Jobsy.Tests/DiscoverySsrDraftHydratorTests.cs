using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Discovery;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class DiscoverySsrDraftHydratorTests
{
    [Fact]
    public void Overview_rots_summaries_use_profile_and_private_preferences()
    {
        var culture = new CultureState(null!, null!, null!);
        var profile = new MeProfile
        {
            Preferences = new CandidatePreferences
            {
                Roles = ["horeca"],
                Educations = ["HBO – Kok"],
                Hobbies = ["sport"],
                Employers =
                [
                    new CandidateEmployerHistory { EmployerName = "Restaurant De Haven" },
                ],
            },
        };
        var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
        OnboardingProfileDraftHydrator.ApplyPrivatePreferences(
            draft,
            new CandidatePrivatePreferences { Dislikes = ["night-shifts"] });
        OnboardingProfileDraftHydrator.ApplyDreamFromCareerPath(
            draft,
            new CareerPathPlanApiModel { DreamTitle = "Chef-kok" });

        Assert.DoesNotContain(
            culture["Discovery.Overview.SummaryEmpty"],
            DiscoverySsrOverviewSummaries.StepOverviewSummary(3, culture, draft, null),
            StringComparison.Ordinal);
        Assert.Contains("HBO", DiscoverySsrOverviewSummaries.StepOverviewSummary(4, culture, draft, null), StringComparison.Ordinal);
        Assert.DoesNotContain(
            culture["Discovery.Overview.SummaryEmpty"],
            DiscoverySsrOverviewSummaries.StepOverviewSummary(5, culture, draft, null),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            culture["Discovery.Overview.SummaryEmpty"],
            DiscoverySsrOverviewSummaries.StepOverviewSummary(6, culture, draft, null),
            StringComparison.Ordinal);
    }
}
