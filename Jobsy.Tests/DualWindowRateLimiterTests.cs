using System.Threading.RateLimiting;
using Jobsy.Api.Security;

namespace Jobsy.Tests;

public class DualWindowRateLimiterTests
{
    [Fact]
    public void Dispose_twice_is_safe_and_releases_leases()
    {
        var limiter = new DualWindowRateLimiter(
            shortPermitLimit: 2,
            shortWindow: TimeSpan.FromMinutes(1),
            longPermitLimit: 5,
            longWindow: TimeSpan.FromHours(1));

        using (var lease = limiter.AttemptAcquire(1))
        {
            Assert.True(lease.IsAcquired);
        }

        limiter.Dispose();
        limiter.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_twice_is_safe()
    {
        var limiter = new DualWindowRateLimiter(
            shortPermitLimit: 1,
            shortWindow: TimeSpan.FromMinutes(1),
            longPermitLimit: 2,
            longWindow: TimeSpan.FromHours(1));

        using (var lease = await limiter.AcquireAsync(1))
        {
            Assert.True(lease.IsAcquired);
        }

        await limiter.DisposeAsync();
        await limiter.DisposeAsync();
    }

    [Fact]
    public void Acquired_pair_lease_can_be_disposed()
    {
        var limiter = new DualWindowRateLimiter(
            shortPermitLimit: 3,
            shortWindow: TimeSpan.FromMinutes(1),
            longPermitLimit: 3,
            longWindow: TimeSpan.FromHours(1));

        var lease = limiter.AttemptAcquire(1);
        Assert.True(lease.IsAcquired);
        lease.Dispose();
        lease.Dispose();
        limiter.Dispose();
    }
}
