using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;

namespace Jobsy.Tests.Passport;

public class PassportPdfMatchCardCopyTests
{
    private static readonly Guid VacancyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void Student_rules_exclude_adult_with_two_employers()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: ["Orderpicker"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets",
            Educations: ["MBO Logistiek niveau 2"],
            Employers:
            [
                new CandidateEmployerHistoryDto("GreenPack", "Orderpicker", StartMonth: "2022-03", EndMonth: "2024-08"),
                new CandidateEmployerHistoryDto("Westland Fresh DC", "Magazijnmedewerker", StartMonth: "2024-09")
            ]);

        Assert.False(StudentPassportRules.Qualifies(prefs));
        Assert.True(StudentPassportRules.HasSubstantialWorkExperience(prefs));
    }

    [Fact]
    public void Match_copy_dedupes_and_strips_scores()
    {
        var matches = new List<CandidateMatchedVacancyDto>
        {
            NewMatch("Activiteitenbegeleider", "Op Innovatie scoor jij 75%. Dat past goed…"),
            NewMatch("Activiteitenbegeleider in Seghwaert (Zoetermeer)", "Nog een score 80%"),
            NewMatch("Activiteitenbegeleider (43)", "Bedrijf X")
        };

        var fits = PassportPdfMatchCardCopy.ToJobFits(matches, "nl", take: 3);
        Assert.Single(fits);
        Assert.Equal("Activiteitenbegeleider", fits[0].Title);
        Assert.DoesNotContain("%", fits[0].Why);
        Assert.DoesNotContain("scoor", fits[0].Why, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bedrijf X", fits[0].Why);
    }

    private static CandidateMatchedVacancyDto NewMatch(string title, string whyLine)
        => new(
            VacancyId,
            title,
            "Bedrijf X",
            null,
            null,
            0,
            "green",
            [whyLine],
            [],
            false,
            null);
}
