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

    [Fact]
    public void Match_copy_dedupes_by_title_and_employer_for_different_ids()
    {
        var matches = new List<CandidateMatchedVacancyDto>
        {
            NewMatch(Guid.Parse("11111111-1111-1111-1111-111111111101"), "Flex: Keukenhulp Transvaal", "Demo BV", []),
            NewMatch(Guid.Parse("11111111-1111-1111-1111-111111111102"), "Flex: Keukenhulp Transvaal", "Demo BV", []),
            NewMatch(Guid.Parse("11111111-1111-1111-1111-111111111103"), "Orderpicker", "Fresh DC", [])
        };

        var fits = PassportPdfMatchCardCopy.ToJobFits(matches, "nl", maxTravelMinutes: 35, take: 3);
        Assert.Equal(2, fits.Count);
        Assert.Contains(fits, f => f.Title == "Orderpicker");
        Assert.Contains(fits, f => f.Title == "Flex: Keukenhulp Transvaal");
    }

    [Fact]
    public void Match_copy_ignores_engine_why_and_uses_factual_line()
    {
        var matches = new List<CandidateMatchedVacancyDto>
        {
            NewMatch(
                Guid.NewGuid(),
                "Magazijnmedewerker in Naaldwijk",
                "Westland Fresh DC",
                ["Jouw sterke probleemoplossen uit Wie ben ik? past bij wat deze baan vraagt."])
        };

        var fits = PassportPdfMatchCardCopy.ToJobFits(matches, "nl", maxTravelMinutes: 30);
        Assert.Single(fits);
        Assert.Contains("Westland Fresh DC", fits[0].Why, StringComparison.Ordinal);
        Assert.Contains("Naaldwijk", fits[0].Why, StringComparison.Ordinal);
        Assert.Contains("30 min", fits[0].Why, StringComparison.Ordinal);
        Assert.DoesNotContain("Wie ben ik", fits[0].Why, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sterke", fits[0].Why, StringComparison.OrdinalIgnoreCase);
    }

    private static CandidateMatchedVacancyDto NewMatch(string title, string whyLine)
        => NewMatch(VacancyId, title, "Bedrijf X", [whyLine]);

    private static CandidateMatchedVacancyDto NewMatch(
        Guid id,
        string title,
        string company,
        IReadOnlyList<string> why)
        => new(
            id,
            title,
            company,
            null,
            null,
            0,
            "green",
            why,
            [],
            false,
            null);
}
