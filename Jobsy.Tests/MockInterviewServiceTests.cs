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
}
