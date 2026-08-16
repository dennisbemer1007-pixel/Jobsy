using System.Net;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class JobsyApiTransientRetryHandlerTests
{
    [Fact]
    public async Task Get_retries_unauthorized_then_succeeds()
    {
        var inner = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
        var sut = new JobsyApiTransientRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(sut);
        var response = await client.GetAsync("http://retry.test/api/metrics/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Post_is_not_retried()
    {
        var inner = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new JobsyApiTransientRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(sut);
        var response = await client.PostAsync("http://retry.test/api/applications", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Get_does_not_retry_unauthorized_when_auth_headers_were_sent()
    {
        var inner = new InspectingHandler((request, _) =>
        {
            request.Headers.TryAddWithoutValidation("X-Jobsy-Email", "admin@jobsy.local");
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });
        var sut = new JobsyApiTransientRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(sut);
        var response = await client.GetAsync("http://retry.test/api/metrics/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Get_retry_uses_a_fresh_request_without_stale_auth_headers()
    {
        HttpRequestMessage? first = null;
        HttpRequestMessage? second = null;
        var inner = new InspectingHandler((request, call) =>
        {
            if (call == 1)
            {
                first = request;
                request.Headers.TryAddWithoutValidation("X-Jobsy-Email", "stale@jobsy.local");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer stale");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            second = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        });
        var sut = new JobsyApiTransientRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(sut);
        var response = await client.GetAsync("http://retry.test/api/metrics/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.False(second!.Headers.Contains("X-Jobsy-Email"));
        Assert.False(second.Headers.Contains("Authorization"));
    }

    [Fact]
    public void Transient_codes_include_auth_settle_and_gateway()
    {
        Assert.True(JobsyApiTransientRetryHandler.IsTransient(HttpStatusCode.Unauthorized));
        Assert.True(JobsyApiTransientRetryHandler.IsTransient(HttpStatusCode.ServiceUnavailable));
        Assert.False(JobsyApiTransientRetryHandler.IsTransient(HttpStatusCode.Forbidden));
        Assert.False(JobsyApiTransientRetryHandler.IsTransient(HttpStatusCode.NotFound));

        using var anonymous = new HttpRequestMessage(HttpMethod.Get, "http://retry.test/api/me");
        Assert.True(JobsyApiTransientRetryHandler.ShouldRetry(HttpStatusCode.Unauthorized, anonymous));

        using var authed = new HttpRequestMessage(HttpMethod.Get, "http://retry.test/api/me");
        authed.Headers.TryAddWithoutValidation("X-Jobsy-Email", "admin@jobsy.local");
        Assert.False(JobsyApiTransientRetryHandler.ShouldRetry(HttpStatusCode.Unauthorized, authed));
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            var response = responses[Math.Min(_index, responses.Length - 1)];
            _index++;
            return Task.FromResult(response);
        }
    }

    private sealed class InspectingHandler(Func<HttpRequestMessage, int, HttpResponseMessage> onSend)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(onSend(request, Calls));
        }
    }
}
