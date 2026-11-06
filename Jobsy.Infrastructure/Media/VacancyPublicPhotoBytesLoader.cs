using System.Net.Http.Headers;
using Jobsy.Core;
using Jobsy.Core.Contracts;
using Jobsy.Core.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Media;

public sealed class VacancyPublicPhotoBytesLoader : IVacancyPublicPhotoBytesLoader
{
    public const string HttpClientName = "VacancyPublicPhotoBytes";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VacancyPublicPhotoBytesLoader> _logger;

    public VacancyPublicPhotoBytesLoader(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VacancyPublicPhotoBytesLoader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]?> LoadForMapPopupAsync(
        VacancyDiscoveryRecord record,
        Guid vacancyId,
        string? workType,
        CancellationToken cancellationToken = default)
    {
        if (VacancyImageUrls.TryDecodeInlineImage(record.ImageUrl, out var inline, out _))
        {
            return inline;
        }

        var attempted = new List<string>();
        foreach (var path in CandidatePaths(record, vacancyId, workType))
        {
            attempted.Add(path);
            var bytes = await TryLoadPathAsync(path, cancellationToken);
            if (bytes is { Length: > 0 })
            {
                return bytes;
            }
        }

        var publicImage = VacancyImageUrls.PublicImagePath(vacancyId);
        if (!string.IsNullOrWhiteSpace(publicImage))
        {
            attempted.Add(publicImage);
            var viaWebApi = await TryFetchPublicWebAsync(publicImage, cancellationToken);
            if (viaWebApi is { Length: > 0 })
            {
                return viaWebApi;
            }
        }

        _logger.LogWarning(
            "Map popup photo: no source bytes for vacancy {VacancyId}. Tried paths [{Paths}]. PublicWebBaseUrl configured={ConfiguredOrigin}. Resolved origin={ResolvedOrigin}",
            vacancyId,
            string.Join(", ", attempted),
            string.IsNullOrWhiteSpace(_configuration["PublicWebBaseUrl"]) ? "(empty)" : _configuration["PublicWebBaseUrl"],
            VacancyPublicWebOrigin.Resolve(_configuration));

        return null;
    }

    private static IEnumerable<string> CandidatePaths(
        VacancyDiscoveryRecord record,
        Guid vacancyId,
        string? workType)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in OrderedCandidates(record, vacancyId, workType))
        {
            if (seen.Add(path))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> OrderedCandidates(
        VacancyDiscoveryRecord record,
        Guid vacancyId,
        string? workType)
    {
        var direct = VacancyImageUrls.Normalize(record.ImageUrl);
        if (IsStaticPath(direct))
        {
            yield return direct!;
        }

        var card = VacancyImageUrls.ForCard(record.ImageUrl, record.CompanyLogoUrl, vacancyId, workType);
        if (IsStaticPath(card))
        {
            yield return card;
        }

        if (card.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var placeholder = VacancyImageUrls.Placeholder(vacancyId, workType);
            if (IsStaticPath(placeholder))
            {
                yield return placeholder;
            }

            var logo = VacancyImageUrls.Normalize(record.CompanyLogoUrl);
            if (IsStaticPath(logo))
            {
                yield return logo!;
            }
        }

        var photo = VacancyImageUrls.Normalize(record.ImageUrl);
        if (IsStaticPath(photo) && !string.Equals(photo, card, StringComparison.OrdinalIgnoreCase))
        {
            yield return photo!;
        }
    }

    private static bool IsStaticPath(string? path)
        => !string.IsNullOrWhiteSpace(path)
           && VacancyImageUrls.IsSafeSameOriginPath(path!)
           && !path!.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

    private async Task<byte[]?> TryLoadPathAsync(string webPath, CancellationToken cancellationToken)
    {
        var fromDisk = await TryReadWwwrootFileAsync(webPath, cancellationToken);
        if (fromDisk is { Length: > 0 })
        {
            return fromDisk;
        }

        return await TryFetchPublicWebAsync(webPath, cancellationToken);
    }

    private async Task<byte[]?> TryFetchPublicWebAsync(string webPath, CancellationToken cancellationToken)
    {
        if (!VacancyImageUrls.IsSafeSameOriginPath(webPath))
        {
            _logger.LogWarning("Map popup photo fetch skipped: unsafe path {Path}", webPath);
            return null;
        }

        var baseUrl = VacancyPublicWebOrigin.Resolve(_configuration);
        var url = baseUrl.TrimEnd('/') + webPath;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            request.Headers.UserAgent.ParseAdd("Jobsy-MapPopupPhoto/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Map popup photo fetch failed: GET {Url} returned {StatusCode}",
                    url,
                    (int)response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
            {
                _logger.LogWarning("Map popup photo fetch returned empty body for {Url}", url);
                return null;
            }

            return bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Map popup photo fetch error for GET {Url}", url);
            return null;
        }
    }

    internal static async Task<byte[]?> TryReadWwwrootFileAsync(string webPath, CancellationToken cancellationToken)
    {
        if (!VacancyImageUrls.IsSafeSameOriginPath(webPath))
        {
            return null;
        }

        var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var roots = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Jobsy.Web", "wwwroot")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Jobsy.Web", "wwwroot"))
        };
        var repoWebRoot = TryFindRepoWebRoot();
        if (repoWebRoot is not null)
        {
            roots.Add(repoWebRoot);
        }

        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(full))
            {
                return await File.ReadAllBytesAsync(full, cancellationToken);
            }
        }

        return null;
    }

    internal Task<byte[]?> LoadPathViaPublicWebForTestsAsync(string webPath, CancellationToken cancellationToken)
        => TryFetchPublicWebAsync(webPath, cancellationToken);

    private static string? TryFindRepoWebRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Jobsy.Web", "wwwroot");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                break;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
