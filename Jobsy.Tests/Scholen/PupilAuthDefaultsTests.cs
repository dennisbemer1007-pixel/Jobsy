using Jobsy.Core.Authorization;
using Xunit;

namespace Jobsy.Tests.Scholen;

public sealed class PupilAuthDefaultsTests
{
    [Fact]
    public void IdleTimeout_is_thirty_minutes_sliding()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), PupilAuthDefaults.IdleTimeout);
    }

    [Fact]
    public void AbsoluteTimeout_stays_two_hours()
    {
        Assert.Equal(TimeSpan.FromMinutes(120), PupilAuthDefaults.AbsoluteTimeout);
    }
}
