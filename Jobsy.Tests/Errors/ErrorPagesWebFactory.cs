using Jobsy.Core.Features;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Web host for the status / error pages. Every outbound dependency is replaced by one that
/// throws, so a green test proves the pages render without the API (errors 01 success criteria).
/// </summary>
public sealed class ErrorPagesWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public const string ThrowPath = "/__test/throw";

    /// <summary>Replaces <see cref="IErrorChromeProvider"/> with one that throws (layout fallback).</summary>
    public bool BreakLayout { get; init; }

    public bool EmployersEnabled { get; init; }

    /// <summary>
    /// By default every API call throws. Pages that legitimately call the API (the vacancy
    /// detail page) need a host where the API answers 404 instead.
    /// </summary>
    public bool ApiAnswersNotFound { get; init; }

    /// <summary>Mirrors <c>Seo:NoIndex</c> / <c>Seo__NoIndex</c> on Acceptatie.</summary>
    public bool NoIndex { get; init; }

    public RecordingLoggerProvider Logs { get; } = new();

    public HttpClient CreateHtmlClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["Support:Email"] = "support@lobsy.nl",
                // Development keeps the developer exception page; opt in to the production handler.
                [ErrorPagesExtensions.ForceHandlerConfigKey] = "true",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                ["Seo:NoIndex"] = NoIndex ? "true" : "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(Logs);

            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(new NoopForwarder());

            services.RemoveAll<IEmployersSwitch>();
            services.AddSingleton<IEmployersSwitch>(new FixedEmployersSwitch(EmployersEnabled));
            services.RemoveAll<IFeatureFlags>();
            services.AddSingleton<IFeatureFlags>(new FixedFeatureFlags(EmployersEnabled));

            // Any API call from an error page is a bug: make it loud.
            var apiAnswersNotFound = ApiAnswersNotFound;
            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(sp => new JobsyApiClient(
                new HttpClient(CreateApiHandler(apiAnswersNotFound)) { BaseAddress = new Uri("http://api.test/") },
                sp.GetRequiredService<MeGetCache>()));
            services.AddHttpClient(AuthApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => CreateApiHandler(apiAnswersNotFound))
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://api.test/"));

            if (BreakLayout)
            {
                services.RemoveAll<IErrorChromeProvider>();
                services.AddSingleton<IErrorChromeProvider>(new ExplodingChromeProvider());
            }

            services.AddSingleton<IStartupFilter>(new ThrowEndpointStartupFilter());
        });
    }

    /// <summary>Appends a path that always throws, after routing, so the exception handler sees it.</summary>
    private sealed class ThrowEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                next(app);
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Path.StartsWithSegments(ThrowPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Secret boom detail from the database connection string.");
                    }

                    await nextMiddleware(context);
                });
            };
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(enabled);

        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class ExplodingChromeProvider : IErrorChromeProvider
    {
        public ErrorChrome Build(HttpContext? http)
            => throw new InvalidOperationException("Chrome is broken too.");
    }

    private static HttpMessageHandler CreateApiHandler(bool answerNotFound)
        => answerNotFound ? new NotFoundHandler() : new ExplodingHandler();

    private sealed class ExplodingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Error pages must not call the API.");
    }

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            {
                RequestMessage = request
            });
    }
}

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _messages = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_gate)
            {
                return [.. _messages];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new Recorder(this);

    public void Dispose()
    {
    }

    private void Add(string message)
    {
        lock (_gate)
        {
            _messages.Add(message);
        }
    }

    private sealed class Recorder(RecordingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => owner.Add(formatter(state, exception));
    }
}
