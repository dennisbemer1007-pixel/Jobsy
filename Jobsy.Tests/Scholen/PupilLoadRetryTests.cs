using System.Net;
using Jobsy.Web.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilLoadRetryTests
{
    [Fact]
    public void Parses_retryAfterSeconds_from_problem_json_body()
    {
        var ex = new HttpRequestException(
            """pupil progress failed: 429 {"code":"rate_limited","retryAfterSeconds":60}""",
            null,
            HttpStatusCode.TooManyRequests);

        Assert.Equal(60, PupilLoadRetry.TryParseRetryAfterSeconds(ex));
    }

    [Fact]
    public void Caps_rate_limit_wait_at_three_seconds()
    {
        var ex = new HttpRequestException(
            """429 {"retryAfterSeconds":120}""",
            null,
            HttpStatusCode.TooManyRequests);

        Assert.Equal(PupilLoadRetry.MaxRateLimitWait, PupilLoadRetry.ResolveRateLimitWait(ex));
    }

    [Fact]
    public async Task Returns_rate_limited_when_progress_stays_on_429()
    {
        Task<string?> Fetch() => throw new HttpRequestException(
            """429 {"retryAfterSeconds":90}""",
            null,
            HttpStatusCode.TooManyRequests);

        var outcome = await PupilLoadRetry.RunAsync(Fetch, CancellationToken.None);

        Assert.True(outcome.IsRateLimited);
        Assert.False(outcome.IsOffline);
        Assert.Null(outcome.Value);
    }

    [Fact]
    public async Task Retries_after_bounded_429_wait_and_succeeds()
    {
        var calls = 0;
        Task<string?> Fetch()
        {
            calls++;
            if (calls == 1)
            {
                throw new HttpRequestException(
                    """429 {"retryAfterSeconds":1}""",
                    null,
                    HttpStatusCode.TooManyRequests);
            }

            return Task.FromResult<string?>("ok");
        }

        var outcome = await PupilLoadRetry.RunAsync(Fetch, CancellationToken.None);

        Assert.Equal("ok", outcome.Value);
        Assert.False(outcome.IsRateLimited);
        Assert.True(calls >= 2);
    }
}
