using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Jobsy.Tests;

public class AiRouteLoggingTests
{
    [Fact]
    public async Task Logs_the_eu_host_and_model_and_not_the_prompt()
    {
        var logger = new CollectingLogger();
        var inner = new RecordingHandler();
        var handler = new Jobsy.Infrastructure.Services.AiRouteLoggingHandler(logger)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);
        const string prompt = "SECRET-PROMPT-XYZ";
        var body = $$"""{"model":"mistral-small-latest","messages":[{"role":"user","content":"{{prompt}}"}]}""";
        using var response = await client.PostAsync(
            "https://api.eu.mistral.ai/v1/chat/completions",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var line = Assert.Single(logger.Lines);
        Assert.Contains("api.eu.mistral.ai", line, StringComparison.Ordinal);
        Assert.Contains("mistral-small-latest", line, StringComparison.Ordinal);
        Assert.DoesNotContain(prompt, line, StringComparison.Ordinal);
        Assert.Contains(prompt, inner.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Does_not_log_a_non_ai_host()
    {
        var logger = new CollectingLogger();
        var handler = new Jobsy.Infrastructure.Services.AiRouteLoggingHandler(logger)
        {
            InnerHandler = new RecordingHandler()
        };
        using var client = new HttpClient(handler);
        await client.GetAsync("https://api.mollie.com/v2/methods");

        Assert.Empty(logger.Lines);
    }

    [Theory]
    [InlineData("https://api.eu.mistral.ai/v1/models", true)]
    [InlineData("https://api.mistral.ai/v1/chat/completions", true)]
    [InlineData("https://api.openai.com/v1/chat/completions", true)]
    [InlineData("https://api.mollie.com/v2/methods", false)]
    public void Host_filter_matches_only_ai_endpoints(string url, bool expected)
        => Assert.Equal(expected, Jobsy.Infrastructure.Services.AiCallRoute.IsAiHost(new Uri(url)));

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class CollectingLogger : ILogger<Jobsy.Infrastructure.Services.AiRouteLoggingHandler>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Lines.Add(formatter(state, exception));
    }
}
