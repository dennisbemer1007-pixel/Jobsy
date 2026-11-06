using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class WorkStatusCatalogTests
{
    [Fact]
    public void Sanitize_drops_unknown_codes()
    {
        var result = CandidatePreferencesValidator.Sanitize(
            new CandidatePreferencesDto([], null, null, WorkStatuses: ["student", "bogus", "scholier"]));

        Assert.Equal(["scholier", "student"], result.WorkStatuses!.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Prefers_internships_for_student_codes()
    {
        Assert.True(WorkStatusCatalog.PrefersInternshipsOrSideJobs(["stagiair"]));
        Assert.False(WorkStatusCatalog.PrefersInternshipsOrSideJobs(["loondienst-fulltime"]));
    }
}
