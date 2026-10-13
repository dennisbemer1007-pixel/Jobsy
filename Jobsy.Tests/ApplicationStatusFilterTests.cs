using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class ApplicationStatusFilterTests
{
    [Theory]
    [InlineData("Pending", "open")]
    [InlineData("Accepted", "running")]
    [InlineData("EmployerContacting", "running")]
    [InlineData("Hired", "running")]
    [InlineData("Rejected", "rejected")]
    [InlineData("FilledElsewhere", "rejected")]
    [InlineData("Withdrawn", "rejected")]
    [InlineData("Unknown", null)]
    [InlineData(null, null)]
    public void FilterGroup_maps_status_buckets(string? status, string? expected)
    {
        Assert.Equal(expected, ApplicationStatusWizard.FilterGroup(status));
        Assert.Equal(expected == "open", ApplicationStatusWizard.IsOpen(status));
        Assert.Equal(expected == "running", ApplicationStatusWizard.IsRunning(status));
        Assert.Equal(expected == "rejected", ApplicationStatusWizard.IsRejectedFilter(status));
    }
}
