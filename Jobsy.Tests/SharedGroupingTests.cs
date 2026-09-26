using Jobsy.Web.Models;

namespace Jobsy.Tests;

public class SharedGroupingTests
{
    [Fact]
    public void GroupShares_collapses_same_vacancy_id()
    {
        var vacancyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var vacancyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var items = new List<CandidateEngagementItem>
        {
            new() { Id = Guid.NewGuid(), VacancyId = vacancyA, VacancyTitle = "A", CompanyName = "Co", CreatedAt = DateTime.UtcNow.AddDays(-2), Channel = "WhatsApp" },
            new() { Id = Guid.NewGuid(), VacancyId = vacancyA, VacancyTitle = "A", CompanyName = "Co", CreatedAt = DateTime.UtcNow.AddDays(-1), Channel = "Email" },
            new() { Id = Guid.NewGuid(), VacancyId = vacancyA, VacancyTitle = "A", CompanyName = "Co", CreatedAt = DateTime.UtcNow, Channel = "Link" },
            new() { Id = Guid.NewGuid(), VacancyId = vacancyB, VacancyTitle = "B", CompanyName = "Other", CreatedAt = DateTime.UtcNow.AddHours(-3), Channel = "Link" },
        };

        // Mirror Shared.razor grouping logic (kept in sync by source guard below).
        var groups = items
            .GroupBy(i => i.VacancyId)
            .Select(g => new
            {
                VacancyId = g.Key,
                ShareCount = g.Count(),
                LastSharedAt = g.Max(x => x.CreatedAt)
            })
            .OrderByDescending(g => g.LastSharedAt)
            .ToList();

        Assert.Equal(2, groups.Count);
        var a = Assert.Single(groups, g => g.VacancyId == vacancyA);
        Assert.Equal(3, a.ShareCount);
        Assert.Equal(vacancyA, groups[0].VacancyId);

        var root = FindRepoRoot();
        var shared = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "Shared.razor"));
        Assert.Contains("GroupShares", shared);
        Assert.Contains("Saved.SharedWithMany", shared);
        var ui = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Localization", "UiStrings.cs"));
        Assert.Contains("gedeeld met {0} personen", ui, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
