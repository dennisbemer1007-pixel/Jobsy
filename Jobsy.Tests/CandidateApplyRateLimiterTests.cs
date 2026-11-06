using System.Threading.RateLimiting;
using Jobsy.Api.Security;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class CandidateApplyRateLimiterTests
{
    [Fact]
    public void Ten_applies_per_hour_pass_thirty_first_is_rejected_as_hour_window()
    {
        using var limiter = new TripleWindowRateLimiter(
            burstPermitLimit: 100,
            burstWindow: TimeSpan.FromMinutes(1),
            hourlyPermitLimit: 30,
            hourlyWindow: TimeSpan.FromHours(1),
            dailyPermitLimit: 100,
            dailyWindow: TimeSpan.FromDays(1));

        for (var i = 0; i < 30; i++)
        {
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
        }

        using var blocked = limiter.AttemptAcquire(1);
        Assert.False(blocked.IsAcquired);
        Assert.True(blocked.TryGetMetadata(TripleWindowRateLimiter.LimitWindowMetadata, out var window));
        Assert.Equal("hour", window);
    }

    [Fact]
    public void Ten_applies_within_hour_pass_when_limit_is_thirty()
    {
        using var limiter = new TripleWindowRateLimiter(
            burstPermitLimit: 100,
            burstWindow: TimeSpan.FromMinutes(1),
            hourlyPermitLimit: 30,
            hourlyWindow: TimeSpan.FromHours(1),
            dailyPermitLimit: 100,
            dailyWindow: TimeSpan.FromDays(1));

        for (var i = 0; i < 10; i++)
        {
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
        }
    }

    [Fact]
    public void Sixth_apply_within_one_minute_hits_burst_window()
    {
        using var limiter = new TripleWindowRateLimiter(
            burstPermitLimit: 5,
            burstWindow: TimeSpan.FromMinutes(1),
            hourlyPermitLimit: 30,
            hourlyWindow: TimeSpan.FromHours(1),
            dailyPermitLimit: 100,
            dailyWindow: TimeSpan.FromDays(1));

        for (var i = 0; i < 5; i++)
        {
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
        }

        using var blocked = limiter.AttemptAcquire(1);
        Assert.False(blocked.IsAcquired);
        Assert.True(blocked.TryGetMetadata(TripleWindowRateLimiter.LimitWindowMetadata, out var window));
        Assert.Equal("minute", window);
    }

    [Fact]
    public async Task Apply_rate_limit_rejection_returns_friendly_candidate_code_and_detail()
    {
        using var limiter = new TripleWindowRateLimiter(
            burstPermitLimit: 100,
            burstWindow: TimeSpan.FromMinutes(1),
            hourlyPermitLimit: 1,
            hourlyWindow: TimeSpan.FromHours(1),
            dailyPermitLimit: 100,
            dailyWindow: TimeSpan.FromDays(1));

        using (limiter.AttemptAcquire(1))
        {
        }

        using var rejectedLease = limiter.AttemptAcquire(1);
        Assert.False(rejectedLease.IsAcquired);

        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/api/applications";
        http.Request.Headers.AcceptLanguage = "nl-NL";
        http.Response.Body = new MemoryStream();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();

        await RateLimitPartitioning.OnRejectedAsync(
            new OnRejectedContext { HttpContext = http, Lease = rejectedLease },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        using var reader = new StreamReader(http.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains(RateLimitPartitioning.CandidateApplyRateLimitedCode, body, StringComparison.Ordinal);
        Assert.Contains("Je hebt veel gesolliciteerd", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_apply_rate_limited_maps_to_apply_ui_string()
    {
        Assert.Equal(
            "Apply.RateLimited",
            UserFacingError.MessageKeyFor(new ApiErrorException(ApiErrorException.CandidateApplyRateLimited, 429)));
    }

    [Fact]
    public void Apply_rate_limit_strings_exist_in_all_ui_languages()
    {
        const string nlSnippet = "Probeer het over een uurtje opnieuw";
        Assert.Contains(nlSnippet, UiStrings.Get("Apply.RateLimited", "nl"), StringComparison.Ordinal);
        Assert.Contains("about an hour", UiStrings.Get("Apply.RateLimited", "en"), StringComparison.Ordinal);
        Assert.Contains("godzinę", UiStrings.Get("Apply.RateLimited", "pl"), StringComparison.Ordinal);
        Assert.Contains("oră", UiStrings.Get("Apply.RateLimited", "ro"), StringComparison.Ordinal);
        Assert.Contains("ساعة", UiStrings.Get("Apply.RateLimited", "ar"), StringComparison.Ordinal);
    }
}
