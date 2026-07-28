using Jobsy.Core.Interfaces;
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
}
