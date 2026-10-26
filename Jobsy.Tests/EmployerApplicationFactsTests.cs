using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class EmployerApplicationFactsTests
{
    [Fact]
    public void Build_compact_preference_json_does_not_split_on_json_colons()
    {
        var app = new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Test",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            PreferencesSummary = """{"roles":["Horeca"],"maxTravelMinutes":30,"preferredTransport":"Fiets"}"""
        };

        var rows = EmployerApplicationFacts.Build(app);

        Assert.Contains(rows, r => r.Label == "Rollen" && r.Value.Contains("Horeca", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Label == "Reistijd" && r.Value.Contains("30", StringComparison.Ordinal));
        Assert.Contains(rows, r => r.Label == "Vervoer" && r.Value.Contains("Fiets", StringComparison.Ordinal));
        Assert.DoesNotContain(rows, r => r.Value.Contains("max 30 min · Fiets", StringComparison.Ordinal));
    }

    [Fact]
    public void ToHoursReadable_ignores_travel_fields()
    {
        const string json = """{"minHoursPerWeek":24,"maxHoursPerWeek":32,"maxTravelMinutes":30,"preferredTransport":"Fiets"}""";
        var hours = ApplicationPreferenceRedaction.ToHoursReadable(json);
        Assert.Contains("24", hours!, StringComparison.Ordinal);
        Assert.Contains("32", hours!, StringComparison.Ordinal);
        Assert.DoesNotContain("Fiets", hours!, StringComparison.Ordinal);
    }
}
