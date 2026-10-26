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

        Assert.Single(rows);
        Assert.Equal("Voorkeur", rows[0].Label);
        Assert.Contains("Horeca", rows[0].Value, StringComparison.Ordinal);
        Assert.DoesNotContain("Uren per week", rows[0].Value, StringComparison.Ordinal);
    }
}
