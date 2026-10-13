using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyUrlFetchService : IExternalVacancyUrlFetchService
{
    public const string HttpClientName = "ExternalVacancyFetch";

    private static readonly Regex ScriptStyleRegex = new(
        @"<(script|style)[^>]*>[\s\S]*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalVacancyUrlFetchService> _logger;

    public ExternalVacancyUrlFetchService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalVacancyUrlFetchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(string Html, string VisibleText)?> FetchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        if (!await IsSafePublicHostAsync(url, cancellationToken))
        {
            return null;
        }

        if (!await IsAllowedByRobotsAsync(url, cancellationToken))
        {
            _logger.LogInformation("Robots.txt disallows fetch for {Host}{Path}", url.Host, url.AbsolutePath);
            return null;
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "LobsyExternalVacancyBot/1.0 (+https://lobsy.nl)");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > CandidateExternalVacancyRules.MaxHtmlBytes)
            {
                return null;
            }

            ms.Write(buffer, 0, read);
        }

        var html = Encoding.UTF8.GetString(ms.ToArray());
        if (CandidateExternalVacancyRules.LooksLikeLoginWall("", html))
        {
            return null;
        }

        var visible = ExtractVisibleText(html);
        if (CandidateExternalVacancyRules.LooksLikeLoginWall(visible, html))
        {
            return null;
        }

        if (visible.Length > CandidateExternalVacancyRules.MaxVisibleTextChars)
        {
            visible = visible[..CandidateExternalVacancyRules.MaxVisibleTextChars];
        }

        return (html, visible);
    }

    internal static string ExtractVisibleText(string html)
    {
        var stripped = ScriptStyleRegex.Replace(html, " ");
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(stripped);
        var text = doc.Body?.TextContent ?? doc.DocumentElement?.TextContent ?? "";
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private async Task<bool> IsAllowedByRobotsAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            var robotsUri = new Uri($"{url.Scheme}://{url.Host}/robots.txt");
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(robotsUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return !RobotsDisallowsPath(body, url.AbsolutePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return true;
        }
    }

    internal static bool RobotsDisallowsPath(string robotsTxt, string path)
    {
        var lines = robotsTxt.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var applies = false;
        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                var agent = line["User-agent:".Length..].Trim();
                applies = agent is "*" or "LobsyExternalVacancyBot";
                continue;
            }

            if (!applies || !line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rule = line["Disallow:".Length..].Trim();
            if (rule.Length == 0)
            {
                continue;
            }

            if (path.StartsWith(rule, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> IsSafePublicHostAsync(Uri url, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(url.Host, out var literal))
        {
            try
            {
                var entries = await Dns.GetHostAddressesAsync(url.Host, cancellationToken);
                return entries.Length > 0 && entries.All(a => !IsBlockedIp(a));
            }
            catch
            {
                return false;
            }
        }

        return !IsBlockedIp(literal);
    }

    internal static bool IsBlockedIp(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 10)
            {
                return true;
            }

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            if (bytes[0] == 127)
            {
                return true;
            }

            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return true;
        }

        return false;
    }
}
