using System.Net;
using ApiOriginMiddleware = Jobsy.Api.Security.CloudflareOriginMiddleware;
using Jobsy.Core.Security;
using Jobsy.Web.Security;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Tests;

public class CloudflareOriginProtectionTests
{
    private const string Secret = "origin-secret-for-tests";

    [Fact]
    public async Task Api_rejects_missing_or_wrong_secret_and_allows_health_and_webhooks()
    {
        var middleware = CreateApi(Secret);
        Assert.Equal(StatusCodes.Status403Forbidden, await Invoke(middleware, "/api/me"));
        Assert.Equal(StatusCodes.Status403Forbidden, await Invoke(middleware, "/api/me", "wrong"));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/api/me", Secret));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/health"));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/health/"));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/api/webhooks/mollie"));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/api/feedback/cursor-webhook"));
        Assert.Equal(StatusCodes.Status403Forbidden, await Invoke(middleware, "/api/external/vacancies"));
    }

    [Fact]
    public async Task Web_healthz_is_exempt_and_pages_require_the_header()
    {
        var middleware = CreateWeb(Secret);
        Assert.Equal(StatusCodes.Status403Forbidden, await Invoke(middleware, "/", web: true));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/", Secret, web: true));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/healthz", web: true));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/HEALTHZ", web: true));
        Assert.Equal(StatusCodes.Status200OK, await Invoke(middleware, "/health", web: true));
    }

    [Fact]
    public async Task Unset_secret_does_not_enforce_and_logs_in_production()
    {
        var apiLog = new ListLogger<ApiOriginMiddleware>();
        var api = CreateApi(secret: null, env: Environments.Production, logger: apiLog);
        Assert.Equal(StatusCodes.Status200OK, await Invoke(api, "/api/me"));
        Assert.Contains(apiLog.Messages, m => m.Contains("unset in Production", StringComparison.Ordinal));
        Assert.DoesNotContain(apiLog.Messages, m => m.Contains(Secret, StringComparison.Ordinal));

        var devLog = new ListLogger<ApiOriginMiddleware>();
        var dev = CreateApi(secret: null, env: Environments.Development, logger: devLog);
        Assert.Equal(StatusCodes.Status200OK, await Invoke(dev, "/api/me"));
        Assert.Empty(devLog.Messages);

        var webLog = new ListLogger<Jobsy.Web.Security.CloudflareOriginMiddleware>();
        var web = CreateWeb(secret: null, env: Environments.Production, logger: webLog);
        Assert.Equal(StatusCodes.Status200OK, await Invoke(web, "/", web: true));
        Assert.Contains(
            webLog.Messages,
            m => m.Contains("unset in Production", StringComparison.Ordinal));
    }

    [Fact]
    public void Newline_in_secret_disables_enforcement_without_logging_the_value()
    {
        var log = new ListLogger<ApiOriginMiddleware>();
        _ = CreateApi("good\r\nsecret", env: Environments.Production, logger: log);
        Assert.Contains(log.Messages, m => m.Contains("not a single-line", StringComparison.Ordinal));
        Assert.DoesNotContain(log.Messages, m => m.Contains("good", StringComparison.Ordinal));
        Assert.Null(CloudflareOriginSecret.Normalize("  line\nbreak "));
        Assert.Equal("trimmed", CloudflareOriginSecret.Normalize("  trimmed  "));
    }

    [Fact]
    public async Task Health_probe_does_not_trust_forged_cloudflare_ip()
    {
        var middleware = CreateApi(Secret);
        var http = new DefaultHttpContext();
        http.Request.Path = "/health";
        http.Request.Headers["CF-Connecting-IP"] = "203.0.113.9";
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");
        await middleware.InvokeAsync(http);
        Assert.Equal(IPAddress.Parse("10.0.0.8"), http.Connection.RemoteIpAddress);

        var trusted = new DefaultHttpContext();
        trusted.Request.Path = "/api/me";
        trusted.Request.Headers[CloudflareOriginSecret.HeaderName] = Secret;
        trusted.Request.Headers["CF-Connecting-IP"] = "203.0.113.9";
        trusted.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");
        await middleware.InvokeAsync(trusted);
        Assert.Equal(IPAddress.Parse("203.0.113.9"), trusted.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task Server_side_handler_sends_secret_only_to_the_api_host()
    {
        var config = Config(("CLOUDFLARE_ORIGIN_SECRET", Secret), ("ApiBaseUrl", "https://api.example.test/"));
        var seen = new List<(string Host, string? Secret)>();
        var inner = new RecordingHandler(req =>
        {
            req.Headers.TryGetValues(CloudflareOriginSecret.HeaderName, out var values);
            seen.Add((req.RequestUri!.Host, values?.SingleOrDefault()));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = new CloudflareOriginHeaderHandler(config) { InnerHandler = inner };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.test/") };

        await client.GetAsync("api/me");
        await client.GetAsync("https://nominatim.openstreetmap.org/search");

        Assert.Equal(("api.example.test", Secret), seen[0]);
        Assert.Equal(("nominatim.openstreetmap.org", null), seen[1]);
    }

    [Fact]
    public async Task Unset_secret_does_not_add_or_strip_an_existing_header()
    {
        var config = Config(("ApiBaseUrl", "https://api.example.test/"));
        string? seen = null;
        var inner = new RecordingHandler(req =>
        {
            seen = req.Headers.TryGetValues(CloudflareOriginSecret.HeaderName, out var values)
                ? values.Single()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = new CloudflareOriginHeaderHandler(config) { InnerHandler = inner };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/api/me");
        request.Headers.TryAddWithoutValidation(CloudflareOriginSecret.HeaderName, "left-alone");
        await client.SendAsync(request);
        Assert.Equal("left-alone", seen);
    }

    [Fact]
    public async Task Retry_still_sends_the_origin_secret()
    {
        var config = Config(("CLOUDFLARE_ORIGIN_SECRET", Secret), ("ApiBaseUrl", "https://api.example.test/"));
        var attempts = new List<string?>();
        var inner = new RecordingHandler(req =>
        {
            req.Headers.TryGetValues(CloudflareOriginSecret.HeaderName, out var values);
            attempts.Add(values?.SingleOrDefault());
            return new HttpResponseMessage(
                attempts.Count == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        });
        var origin = new CloudflareOriginHeaderHandler(config) { InnerHandler = inner };
        var retry = new JobsyApiTransientRetryHandler { InnerHandler = origin };
        using var client = new HttpClient(retry) { BaseAddress = new Uri("https://api.example.test/") };
        var response = await client.GetAsync("api/settings/session-security");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { Secret, Secret }, attempts);
    }

    [Fact]
    public async Task Cross_host_redirect_drops_the_origin_secret()
    {
        var hops = new List<(string Host, string? Secret)>();
        var inner = new RecordingHandler(req =>
        {
            req.Headers.TryGetValues(CloudflareOriginSecret.HeaderName, out var values);
            var host = req.RequestUri!.Host;
            hops.Add((host, values?.SingleOrDefault()));
            if (host == "api.example.test")
            {
                return new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("https://images.example.test/photo.jpg") }
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var redirect = new JobsyApiRedirectHandler { InnerHandler = inner };
        using var client = new HttpClient(redirect);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/api/vacancies/1/image");
        request.Headers.TryAddWithoutValidation(CloudflareOriginSecret.HeaderName, Secret);
        request.Headers.TryAddWithoutValidation(InternalClientIpHeaders.InternalSecretHeader, "internal");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("api.example.test", Secret), hops[0]);
        Assert.Equal(("images.example.test", null), hops[1]);
    }

    [Fact]
    public async Task Web_host_healthz_stays_open_when_origin_secret_is_set()
    {
        await using var factory = new OriginEnforcedWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var health = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("ok", (await health.Content.ReadAsStringAsync()).Trim());

        var blocked = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            CloudflareOriginSecret.HeaderName,
            OriginEnforcedWebFactory.Secret);
        var allowed = await client.GetAsync("/");
        Assert.NotEqual(HttpStatusCode.Forbidden, allowed.StatusCode);
    }

    private static ApiOriginMiddleware CreateApi(
        string? secret,
        string env = "Production",
        ListLogger<ApiOriginMiddleware>? logger = null)
    {
        var config = Config(secret is null ? null : ("CLOUDFLARE_ORIGIN_SECRET", secret));
        return new ApiOriginMiddleware(
            _ => Task.CompletedTask,
            config,
            new FakeEnv(env),
            logger);
    }

    private static Jobsy.Web.Security.CloudflareOriginMiddleware CreateWeb(
        string? secret,
        string env = "Production",
        ListLogger<Jobsy.Web.Security.CloudflareOriginMiddleware>? logger = null)
    {
        var config = Config(secret is null ? null : ("CLOUDFLARE_ORIGIN_SECRET", secret));
        return new Jobsy.Web.Security.CloudflareOriginMiddleware(
            _ => Task.CompletedTask,
            config,
            new FakeEnv(env),
            logger);
    }

    private static IConfiguration Config(params (string Key, string Value)?[] values)
    {
        var data = new Dictionary<string, string?>();
        foreach (var value in values)
        {
            if (value is { } pair)
            {
                data[pair.Key] = pair.Value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static async Task<int> Invoke(
        object middleware,
        string path,
        string? secret = null,
        bool web = false)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();
        if (secret is not null)
        {
            http.Request.Headers[CloudflareOriginSecret.HeaderName] = secret;
        }

        if (web)
        {
            await ((Jobsy.Web.Security.CloudflareOriginMiddleware)middleware).InvokeAsync(http);
        }
        else
        {
            await ((ApiOriginMiddleware)middleware).InvokeAsync(http);
        }

        return http.Response.StatusCode == 0 ? StatusCodes.Status200OK : http.Response.StatusCode;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    private sealed class FakeEnv(string name) : IHostEnvironment, IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

file sealed class OriginEnforcedWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public const string Secret = "web-host-origin-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["CLOUDFLARE_ORIGIN_SECRET"] = Secret,
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
            });
        });
    }
}
