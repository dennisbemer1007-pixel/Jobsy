using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class MockInterviewServiceTests
{
    [Fact]
    public void SanitizeHistory_keeps_only_user_and_assistant_and_trims()
    {
        var input = new List<MockInterviewMessage>
        {
            new("system", "ignore"),
            new("user", "  hallo  "),
            new("assistant", "vraag"),
            new("Candidate", "antwoord"),
        };

        var cleaned = MockInterviewService.SanitizeHistory(input);

        Assert.Equal(3, cleaned.Count);
        Assert.Equal("user", cleaned[0].Role);
        Assert.Equal("hallo", cleaned[0].Content);
        Assert.Equal("assistant", cleaned[1].Role);
        Assert.Equal("user", cleaned[2].Role);
    }

    [Fact]
    public void ScriptedFallback_opens_with_vacancy_specific_question()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Magazijnmedewerker",
            "Je werkt in het magazijn: inpakken van orders en laden van karren. Tempo en netjes werken zijn belangrijk.",
            "Bakkerij De Zon",
            "Kerkstraat 1",
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            null,
            ["Logistiek"]);

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, []);

        Assert.Contains("Magazijnmedewerker", reply, StringComparison.Ordinal);
        Assert.Contains("Bakkerij De Zon", reply, StringComparison.Ordinal);
        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
        Assert.True(
            reply.Contains("magazijn", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("inpak", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("logistiek", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScriptedFallback_gives_sterk_and_tip_after_answer()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Bezorgmedewerker",
            "Je bezorgt bestellingen in de buurt per fiets. Klantvriendelijk en op tijd zijn is belangrijk.",
            "FietsExpress",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            14.50m,
            []);

        var history = new List<MockInterviewMessage>
        {
            new("assistant", "Vraag: Waarom past deze vacature bij jou?"),
            new("user", "Ik woon dichtbij en vind fietsen leuk, plus ik heb al bezorgd bij een webshop.")
        };

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history);

        Assert.Contains("Sterk:", reply, StringComparison.Ordinal);
        Assert.Contains("Tip:", reply, StringComparison.Ordinal);
        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
        Assert.True(
            reply.Contains("bezorg", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("klant", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("ervaring", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("fiets", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScriptedFallback_flags_insulting_answer_friendly_and_reasks()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Kassamedewerker",
            "Je werkt aan de kassa en helpt klanten vriendelijk. Netjes en respectvol werken is belangrijk.",
            "Supermarkt West",
            null,
            new DateOnly(2026, 8, 1),
            [],
            null,
            ["Winkel"]);

        var firstQuestion = MockInterviewService.ScriptedFallback.NextReply(vacancy, []);
        Assert.Contains("Vraag:", firstQuestion, StringComparison.Ordinal);
        var questionText = firstQuestion.Split("Vraag:", 2, StringSplitOptions.None)[1].Trim();

        var history = new List<MockInterviewMessage>
        {
            new("assistant", firstQuestion),
            new("user", "Je bent een idioot, dit is kut.")
        };

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history);

        Assert.Contains("Let op:", reply, StringComparison.Ordinal);
        Assert.Contains("geen nette reactie", reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Probeer zo:", reply, StringComparison.Ordinal);
        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
        Assert.DoesNotContain("Sterk:", reply, StringComparison.Ordinal);
        // Soft re-ask: same theme/question rather than advancing.
        Assert.Contains(questionText, reply, StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptedFallback_vague_answer_gets_rewrite_example()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Magazijnmedewerker",
            "Je werkt in het magazijn: inpakken van orders en laden van karren.",
            "LogiWest",
            null,
            new DateOnly(2026, 8, 1),
            [],
            null,
            ["Logistiek"]);

        var history = new List<MockInterviewMessage>
        {
            new("assistant", "Vraag: Hoe ga jij om met tillen, tempo en netjes werken?"),
            new("user", "Ik vind het wel oké.")
        };

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history);

        Assert.Contains("Sterk:", reply, StringComparison.Ordinal);
        Assert.Contains("Tip:", reply, StringComparison.Ordinal);
        Assert.Contains("Probeer zo:", reply, StringComparison.Ordinal);
        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptedFallback_varies_feedback_for_different_answers()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Bezorgmedewerker",
            "Je bezorgt bestellingen in de buurt per fiets. Klantvriendelijk en op tijd zijn is belangrijk.",
            "FietsExpress",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            14.50m,
            []);

        var a = MockInterviewService.ScriptedFallback.NextReply(vacancy,
        [
            new("assistant", "Vraag: Waarom past deze vacature bij jou?"),
            new("user", "Ik woon dichtbij en vind fietsen leuk, plus ik heb al bezorgd bij een webshop.")
        ]);
        var b = MockInterviewService.ScriptedFallback.NextReply(vacancy,
        [
            new("assistant", "Vraag: Waarom past deze vacature bij jou?"),
            new("user", "Op school fietste ik elke dag 12 km; toen ik te laat dreigde te komen, ging ik eerder weg en kwam ik altijd op tijd.")
        ]);

        Assert.Contains("Sterk:", a, StringComparison.Ordinal);
        Assert.Contains("Sterk:", b, StringComparison.Ordinal);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ScriptedFallback_english_uses_english_labels()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Warehouse helper",
            "You pack orders in the warehouse.",
            "Fresh Co",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            null,
            ["Logistiek"]);

        var reply = MockInterviewService.ScriptedFallback.NextReply(
            vacancy,
            [],
            MockInterviewLabels.For("en"));

        Assert.Contains("Question:", reply, StringComparison.Ordinal);
        Assert.Contains("Hi!", reply, StringComparison.Ordinal);
        Assert.DoesNotContain("Vraag:", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptedFallback_polish_falls_back_to_english_prose()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Barista",
            "Je maakt koffie voor gasten.",
            "Café Zon",
            null,
            new DateOnly(2026, 8, 1),
            [],
            null,
            ["Horeca"]);

        var reply = MockInterviewService.ScriptedFallback.NextReply(
            vacancy,
            [],
            MockInterviewLabels.For("pl"));

        Assert.Contains("Question:", reply, StringComparison.Ordinal);
        Assert.Contains("Hi!", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void ScriptedFallback_receives_funny_answer_warmly_but_keeps_feedback()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Bezorgmedewerker",
            "Je bezorgt bestellingen in de buurt per fiets. Klantvriendelijk en op tijd zijn is belangrijk.",
            "FietsExpress",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            14.50m,
            []);

        var history = new List<MockInterviewMessage>
        {
            new("assistant", "Vraag: Hoe plan jij een route?"),
            new("user", "Haha ik plan mijn route alsof mijn leven ervan afhangt, en check daarna of alles klopt.")
        };

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history);

        Assert.Contains("Sterk:", reply, StringComparison.Ordinal);
        Assert.Contains("Tip:", reply, StringComparison.Ordinal);
        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
        Assert.DoesNotContain("Let op:", reply, StringComparison.Ordinal);
        Assert.True(
            reply.Contains("Haha", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("knipoog", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("Glimlach", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("vrolijk", StringComparison.OrdinalIgnoreCase),
            reply);
        Assert.True(
            reply.Contains("serieus", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("voorbeeld", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("recruiter", StringComparison.OrdinalIgnoreCase),
            reply);
    }

    [Fact]
    public void ScriptedFallback_opens_with_quoted_vacancy_duty()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Magazijnmedewerker",
            "Je werkt in het magazijn: inpakken van orders en laden van karren. Tempo en netjes werken zijn belangrijk.",
            "Bakkerij De Zon",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            null,
            ["Logistiek"]);

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, []);

        Assert.Contains("Vraag:", reply, StringComparison.Ordinal);
        Assert.Contains("In de vacature staat:", reply, StringComparison.Ordinal);
        Assert.Contains("magazijn", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScriptedFallback_asks_about_license_gap_with_candidate_profile()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Bezorgmedewerker",
            "Je bezorgt bestellingen per auto bij klanten in de buurt. Klantvriendelijk en op tijd zijn is belangrijk.",
            "FietsExpress",
            null,
            new DateOnly(2026, 8, 1),
            ["Auto"],
            14.50m,
            [],
            RequiredDrivingLicense: "B");

        var entity = new Vacancy
        {
            Id = vacancy.VacancyId,
            Title = vacancy.Title,
            Description = vacancy.Description,
            RequiredDrivingLicense = "B",
            HourlyWage = 14.50m,
            StartDate = vacancy.StartDate,
            EndDate = vacancy.StartDate.AddMonths(3),
            CompanyId = Guid.NewGuid()
        };
        var prefs = new CandidatePreferencesDto(
            Roles: ["logistiek"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Auto",
            DrivingLicenses: [],
            Educations: ["MBO 2"]);
        var candidate = MockInterviewGapAnalyzer.BuildCandidateContext(prefs, entity);

        Assert.Contains(candidate.Gaps, g => g.Key == "license");

        var opening = MockInterviewService.ScriptedFallback.NextReply(vacancy, [], candidate: candidate);
        Assert.Contains("Vraag:", opening, StringComparison.Ordinal);

        // After first answer, progress until a gap question appears.
        var history = new List<MockInterviewMessage>
        {
            new("assistant", opening),
            new("user", "Ik vind bezorgen leuk en woon dichtbij, ik plan mijn route vooraf.")
        };
        string? gapReply = null;
        for (var i = 0; i < 4; i++)
        {
            var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history, candidate: candidate);
            if (reply.Contains("rijbewijs", StringComparison.OrdinalIgnoreCase))
            {
                gapReply = reply;
                break;
            }

            history.Add(new("assistant", reply));
            history.Add(new("user", $"Ik heb ervaring met tempo en klanten, voorbeeld {i + 1}."));
        }

        Assert.NotNull(gapReply);
        Assert.Contains("rijbewijs", gapReply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Vraag:", gapReply, StringComparison.Ordinal);
    }

    [Fact]
    public void GapAnalyzer_detects_hours_and_education_mismatches()
    {
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Kassamedewerker",
            Description = "Je werkt zelfstandig aan de kassa en helpt klanten.",
            RequiredEducation = "MBO",
            MinHoursPerWeek = 24,
            MaxHoursPerWeek = 32,
            HourlyWage = 13m,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 12, 1),
            CompanyId = Guid.NewGuid()
        };
        var prefs = new CandidatePreferencesDto(
            Roles: ["winkel"],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            Educations: ["VMBO"],
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 12,
            AboutMe: "Ik help graag mensen.");

        var candidate = MockInterviewGapAnalyzer.BuildCandidateContext(prefs, vacancy);

        Assert.Contains(candidate.Gaps, g => g.Key == "education");
        Assert.Contains(candidate.Gaps, g => g.Key == "hours");
        Assert.All(candidate.Gaps, g =>
        {
            Assert.False(string.IsNullOrWhiteSpace(g.Question));
            Assert.False(string.IsNullOrWhiteSpace(g.EnglishQuestion));
        });
    }
}
