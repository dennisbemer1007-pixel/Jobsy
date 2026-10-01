using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Jobsy.Api.Security;
using Jobsy.Web.Hosting;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Tests.Errors;

/// <summary>
/// errors 04 §04.2. The Web answers a rate-limited HTML request with the friendly "Even rustig
/// aan" page and a <c>Retry-After</c>; everything else (and the API) gets ProblemDetails.
/// </summary>
public class RateLimitPageTests
{
    /// <summary>The Web "public-redirect" limiter: 60 per minute per IP on <c>/p/{code}</c>.</summary>
    private const string LimitedPath = "/p/zzz";

    private const int PermitLimit = 60;

    [Fact]
    public async Task Html_request_over_the_limit_gets_the_429_page_with_retry_after()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await ExhaustAsync(client, LimitedPath);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.True(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds >= 1);

        Assert.Contains("Even rustig aan", html, StringComparison.Ordinal);
        Assert.Contains("seconden", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "noindex",
            response.Headers.GetValues("X-Robots-Tag").First(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"LB-[23456789ABCDEFGHJKMNPQRSTVWXYZ]{4}", html);
        Assert.Contains("http-equiv=\"refresh\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Json_request_over_the_limit_gets_problem_details_and_no_html()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await ExhaustAsync(client, LimitedPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("rate_limited", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.GetProperty("retryAfterSeconds").GetInt32() >= 1);
        Assert.True(Jobsy.Web.Diagnostics.SupportCode.IsValid(
            doc.RootElement.GetProperty("supportCode").GetString()));
    }

    [Fact]
    public async Task Rate_limit_log_line_carries_the_support_code_and_no_ip()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();

        await ExhaustAsync(client, LimitedPath);

        var line = Assert.Single(
            factory.Logs.Messages.Where(m => m.Contains("Rate limit rejected", StringComparison.Ordinal)));
        Assert.Matches(@"LB-[23456789ABCDEFGHJKMNPQRSTVWXYZ]{4}", line);
        Assert.Contains("policy=public-redirect", line, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", line, StringComparison.Ordinal);
        Assert.DoesNotContain("::1", line, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_rejection_writes_problem_details_with_code_and_support_code()
    {
        var http = new DefaultHttpContext();
        http.Response.Body = new MemoryStream();
        http.Request.Path = "/api/vacancies";

        await RateLimitPartitioning.OnRejectedAsync(
            new OnRejectedContext { HttpContext = http, Lease = new RejectedLease(TimeSpan.FromSeconds(42)) },
            CancellationToken.None);

        Assert.Equal(429, http.Response.StatusCode);
        Assert.Equal("42", http.Response.Headers.RetryAfter.ToString());
        Assert.Contains("application/problem+json", http.Response.ContentType, StringComparison.Ordinal);

        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("rate_limited", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Too many requests", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("retryAfterSeconds").GetInt32());
        Assert.True(Jobsy.Web.Diagnostics.SupportCode.IsValid(
            doc.RootElement.GetProperty("supportCode").GetString()));
    }

    [Fact]
    public void Retry_after_falls_back_to_a_minute_without_limiter_metadata()
    {
        Assert.Equal(
            RateLimitRejection.DefaultRetryAfterSeconds,
            RateLimitRejection.RetryAfterSeconds(new RejectedLease(null)));
        Assert.Equal(1, RateLimitRejection.RetryAfterSeconds(new RejectedLease(TimeSpan.FromMilliseconds(10))));
    }

    [Fact]
    public void Retry_after_for_a_request_without_a_rejection_is_the_default()
        => Assert.Equal(
            RateLimitRejection.DefaultRetryAfterSeconds,
            RateLimitRejection.RetryAfterFor(new DefaultHttpContext()));

    [Fact]
    public async Task Direct_status_429_also_carries_retry_after()
    {
        await using var factory = new ErrorPagesWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/status/429");

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Contains(
            "Even rustig aan",
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> ExhaustAsync(HttpClient client, string path)
    {
        HttpResponseMessage? response = null;
        for (var i = 0; i <= PermitLimit; i++)
        {
            response?.Dispose();
            response = await client.GetAsync(path);
            if ((int)response.StatusCode == 429)
            {
                return response;
            }
        }

        Assert.Fail($"{path} never hit the rate limit (last status {(int)response!.StatusCode}).");
        return response!;
    }

    /// <summary>A lease that is never acquired, with optional Retry-After metadata.</summary>
    private sealed class RejectedLease(TimeSpan? retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames =>
            retryAfter is null ? [] : [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (retryAfter is not null && metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter.Value;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}
