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
    public void ScriptedFallback_opens_with_vacancy_context()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Magazijnmedewerker",
            "Inpakken en laden",
            "Bakkerij De Zon",
            "Kerkstraat 1",
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            null);

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, []);

        Assert.Contains("Magazijnmedewerker", reply, StringComparison.Ordinal);
        Assert.Contains("Bakkerij De Zon", reply, StringComparison.Ordinal);
        Assert.Contains("oefen", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScriptedFallback_gives_feedback_after_answer()
    {
        var vacancy = new MockInterviewVacancyContext(
            Guid.NewGuid(),
            "Bezorgmedewerker",
            "Bezorgen in de buurt",
            "FietsExpress",
            null,
            new DateOnly(2026, 8, 1),
            ["Fiets"],
            14.50m);

        var history = new List<MockInterviewMessage>
        {
            new("assistant", "Waarom past deze vacature bij jou?"),
            new("user", "Ik woon dichtbij en vind fietsen leuk, plus ik heb al bezorgd bij een webshop.")
        };

        var reply = MockInterviewService.ScriptedFallback.NextReply(vacancy, history);

        Assert.Contains("ervaring", reply, StringComparison.OrdinalIgnoreCase);
    }
}
