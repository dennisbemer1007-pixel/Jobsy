using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class MeGetCacheTests
{
    [Fact]
    public async Task Reuses_value_within_ttl_and_refetches_after_invalidate()
    {
        var cache = new MeGetCache();
        var calls = 0;

        Task<MeGetResult<string>> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult(MeGetResult<string>.Ok("v" + calls));
        }

        var first = await cache.GetOrCreateAsync("k", Factory);
        var second = await cache.GetOrCreateAsync("k", Factory);
        Assert.Equal("v1", first.Value);
        Assert.Equal("v1", second.Value);
        Assert.Equal(1, calls);

        cache.Invalidate("k");
        var third = await cache.GetOrCreateAsync("k", Factory);
        Assert.Equal("v2", third.Value);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Does_not_cache_temporarily_unavailable()
    {
        var cache = new MeGetCache();
        var calls = 0;

        Task<MeGetResult<string>> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult(
                calls == 1
                    ? MeGetResult<string>.Unavailable()
                    : MeGetResult<string>.Ok("ok"));
        }

        var first = await cache.GetOrCreateAsync("k", Factory);
        Assert.True(first.TemporarilyUnavailable);

        var second = await cache.GetOrCreateAsync("k", Factory);
        Assert.Equal("ok", second.Value);
        Assert.Equal(2, calls);
    }
}
