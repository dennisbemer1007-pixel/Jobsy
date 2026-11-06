using Jobsy.Api.Controllers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class SensitiveExperienceRulesTests
{
    [Fact]
    public void Hidden_employer_row_missing_from_employer_view_but_skills_stay_in_matching()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: null,
            PreferredTransport: null,
            Employers:
            [
                new CandidateEmployerHistoryDto("Hotel", "Medewerker", StartMonth: "1994-01", EndMonth: "2003-12"),
                new CandidateEmployerHistoryDto(
                    "Eigen onderneming",
                    "Zelfstandig ondernemer",
                    StartMonth: "2003-01",
                    EndMonth: "2019-12",
                    OnlyForMe: true,
                    Skills: ["Discretie", "Klantcontact"]),
                new CandidateEmployerHistoryDto("Buurthuis", "Gastvrouw", StartMonth: "2024-01")
            ]);

        var visible = SensitiveExperienceRules.VisibleToEmployers(prefs.Employers);
        Assert.Equal(2, visible.Count);
        Assert.DoesNotContain(visible, e => e.Role == "Zelfstandig ondernemer");

        var employerView = EmployerFacingCandidateProfile.FilterPreferences(prefs);
        Assert.Equal(2, employerView.Employers!.Count);

        var skills = EmployerFacingCandidateProfile.VisibleSkillLabels(prefs);
        Assert.Contains("Discretie", skills);
        Assert.Contains("Klantcontact", skills);

        var matchLabels = SensitiveExperienceRules.MatchingExperienceLabels(prefs);
        Assert.Contains("Zelfstandig ondernemer", matchLabels);
    }

    [Fact]
    public void Reference_type_employer_label_never_contains_contact_name()
    {
        var label = ReferenceTypeCatalog.EmployerConfirmationLabel(ReferenceTypeCatalog.Coach, "nl");
        Assert.Equal("een begeleider", label);
        Assert.DoesNotContain("Fatima", label);
    }

    [Fact]
    public void Gap_choice_other_period_normalizes()
    {
        Assert.Equal(
            SensitiveExperienceRules.GapOtherPeriod,
            SensitiveExperienceRules.NormalizeGapChoice("other-period"));
    }

    [Fact]
    public void Parse_and_serialize_only_for_me_roundtrip()
    {
        var json = MeController.SerializePreferences(new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 30,
            PreferredTransport: null,
            Employers:
            [
                new CandidateEmployerHistoryDto(
                    "Eigen onderneming",
                    "Zelfstandig ondernemer",
                    OnlyForMe: true,
                    Skills: ["Klantcontact"])
            ],
            TimelineGaps: [new CandidateTimelineGapDto("2019-01", "2024-01", SensitiveExperienceRules.GapOtherPeriod)]));

        var parsed = MeController.ParsePreferences(json);
        var job = Assert.Single(parsed.Employers!);
        Assert.True(job.OnlyForMe);
        Assert.Equal("Klantcontact", Assert.Single(job.Skills!));
        Assert.Equal(SensitiveExperienceRules.GapOtherPeriod, parsed.TimelineGaps!.Single().Choice);
    }
}
