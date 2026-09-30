using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Live KVK Handelsregister (Zoeken / Basisprofiel / vestigingen). Falls back to
/// <see cref="KvkServiceStub"/> when no API-key is configured so demos keep working.
/// Search and profile responses are cached 24 h (IsOnLobsy / IsInUse recomputed per request).
/// </summary>
public sealed class KvkHandelsregisterService : IKvkService
{
    public const string HttpClientName = "Kvk";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly JobsyDbContext _db;
    private readonly IIntegrationCredentialService _credentials;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KvkServiceStub _stub;
    private readonly ILogger<KvkHandelsregisterService> _logger;
    private readonly IMemoryCache? _cache;
    private readonly IKvkUsageCounter? _usage;

    public KvkHandelsregisterService(
        JobsyDbContext db,
        IIntegrationCredentialService credentials,
        IHttpClientFactory httpClientFactory,
        KvkServiceStub stub,
        ILogger<KvkHandelsregisterService> logger,
        IMemoryCache? cache = null,
        IKvkUsageCounter? usage = null)
    {
        _db = db;
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _stub = stub;
        _logger = logger;
        _cache = cache;
        _usage = usage;
    }

    public async Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (!await HasApiKeyAsync(cancellationToken))
        {
            return await _stub.GetByKvkNumberAsync(kvkNumber, cancellationToken);
        }

        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            var profile = await GetBasisprofielAsync(normalized, cancellationToken);
            return profile is null ? null : MapCompany(normalized, profile);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "KVK basisprofiel failed for {KvkNumber}", normalized);
            return null;
        }
    }

    public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        var lookup = await LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        if (lookup.Status == KvkLookupStatus.Unavailable)
        {
            throw new KvkServiceUnavailableException(lookup.Message ?? "KVK unavailable");
        }

        return lookup.Establishments;
    }

    public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (!await HasApiKeyAsync(cancellationToken))
        {
            return await _stub.LookupEstablishmentsAsync(kvkNumber, cancellationToken);
        }

        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return KvkEstablishmentsLookup.NotFound();
        }

        try
        {
            var profile = await GetBasisprofielAsync(normalized, cancellationToken);
            if (profile is null)
            {
                return KvkEstablishmentsLookup.NotFound();
            }

            var vestigingen = await GetVestigingenAsync(normalized, cancellationToken);
            var sbi = MapSbiCodes(profile.SbiActiviteiten);
            var hqGeo = ExtractHqGeo(profile);
            var hqAddress = FormatEmbeddedHqAddress(profile);
            var legalName = CompanyName(profile);

            var items = MapVestigingen(normalized, vestigingen, legalName, sbi, hqGeo, hqAddress);
            if (items.Count == 0)
            {
                var hq = profile.Embedded?.Hoofdvestiging;
                var vestigingsnummer = DigitsOnly(hq?.Vestigingsnummer);
                if (string.IsNullOrWhiteSpace(vestigingsnummer))
                {
                    return KvkEstablishmentsLookup.NotFound();
                }

                items.Add(new KvkEstablishmentResult(
                    normalized,
                    vestigingsnummer,
                    CompanyPublicPaths.BuildEstablishmentId(normalized, vestigingsnummer),
                    FirstNonEmpty(hq?.EersteHandelsnaam, legalName) ?? legalName,
                    hqAddress,
                    hqGeo.Lat,
                    hqGeo.Lng,
                    false,
                    sbi));
            }

            var inUse = await LoadInUseIdsAsync(normalized, cancellationToken);
            var marked = items
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
                .OrderBy(c => c.EstablishmentNumber, StringComparer.Ordinal)
                .ToList();

            return KvkEstablishmentsLookup.Ok(marked);
        }
        catch (KvkAuthException ex)
        {
            _logger.LogWarning("KVK API-key rejected ({Status}) for {KvkNumber}", ex.StatusCode, normalized);
            return KvkEstablishmentsLookup.Unavailable(
                "KVK weigerde de API-key. Controleer de key én de Base URL (productie vs test) onder Admin → Integraties.");
        }
        catch (KvkServiceUnavailableException ex)
        {
            return KvkEstablishmentsLookup.Unavailable(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "KVK lookup failed for {KvkNumber}", normalized);
            return KvkEstablishmentsLookup.Unavailable();
        }
    }

    public async Task<KvkSearchResult> SearchAsync(
        KvkSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var text = (query.Text ?? string.Empty).Trim();
        var place = string.IsNullOrWhiteSpace(query.Place) ? null : query.Place.Trim();
        var page = query.Page < 1 ? 1 : query.Page;
        var digitOnly = new string(text.Where(char.IsDigit).ToArray());
        var isNumberSearch = digitOnly.Length == 8
                             && text.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c is '.' or '-');

        if (!isNumberSearch && text.Length < 3)
        {
            throw new ArgumentException("Zoekterm moet minimaal 3 tekens zijn.", nameof(query));
        }

        if (!await HasApiKeyAsync(cancellationToken))
        {
            return await _stub.SearchAsync(new KvkSearchQuery(text, place, page), cancellationToken);
        }

        var cacheKey = $"kvk:search:{digitOnly.Length == 8 && isNumberSearch}:{NormalizeCachePart(isNumberSearch ? digitOnly : text)}|{NormalizeCachePart(place)}|{page}";
        CachedSearch? cached = null;
        if (_cache is not null && _cache.TryGetValue(cacheKey, out CachedSearch? hit))
        {
            cached = hit;
        }

        if (cached is null)
        {
            try
            {
                var path = BuildZoekenPath(isNumberSearch ? digitOnly : text, place, page, isNumberSearch);
                var (status, body) = await SendTrackedAsync(
                    path,
                    KvkUsageCallTypes.Zoeken,
                    cancellationToken);

                if ((int)status >= 500 || status == HttpStatusCode.RequestTimeout || (int)status == 429)
                {
                    return KvkSearchResult.Unavailable();
                }

                if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new KvkAuthException(status);
                }

                if (status == HttpStatusCode.NotFound)
                {
                    cached = new CachedSearch([], 0);
                }
                else
                {
                    EnsureSuccess(status, body);
                    cached = ParseZoeken(body);
                }

                _cache?.Set(cacheKey, cached, CacheDuration);
            }
            catch (KvkAuthException)
            {
                return KvkSearchResult.Unavailable(
                    "KVK weigerde de API-key. Controleer de key én de Base URL (productie vs test) onder Admin → Integraties.");
            }
            catch (KvkServiceUnavailableException ex)
            {
                return KvkSearchResult.Unavailable(ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not ArgumentException)
            {
                _logger.LogWarning(ex, "KVK search failed for {Query}", text);
                return KvkSearchResult.Unavailable();
            }
        }

        var onLobsy = await LoadOnLobsyAsync(cached.Hits.Select(h => h.KvkNumber), cancellationToken);
        var marked = cached.Hits
            .Select(h => h with { IsOnLobsy = onLobsy.Contains(h.KvkNumber) })
            .ToList();
        return KvkSearchResult.Ok(marked, cached.Total);
    }

    public async Task<KvkCompanyProfile> GetProfileAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (!await HasApiKeyAsync(cancellationToken))
        {
            return await _stub.GetProfileAsync(kvkNumber, cancellationToken);
        }

        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvkNumber);
        if (normalized is null)
        {
            return KvkCompanyProfile.NotFound(kvkNumber ?? "");
        }

        var cacheKey = $"kvk:profile:{normalized}";
        CachedProfile? cached = null;
        if (_cache is not null && _cache.TryGetValue(cacheKey, out CachedProfile? hit))
        {
            cached = hit;
        }

        if (cached is null)
        {
            try
            {
                var profile = await GetBasisprofielAsync(normalized, cancellationToken);
                if (profile is null)
                {
                    return KvkCompanyProfile.NotFound(normalized);
                }

                var vestigingen = await GetVestigingenAsync(normalized, cancellationToken);
                cached = MapProfile(normalized, profile, vestigingen);
                _cache?.Set(cacheKey, cached, CacheDuration);
            }
            catch (KvkAuthException)
            {
                return KvkCompanyProfile.Unavailable(
                    normalized,
                    "KVK weigerde de API-key. Controleer de key én de Base URL (productie vs test) onder Admin → Integraties.");
            }
            catch (KvkServiceUnavailableException ex)
            {
                return KvkCompanyProfile.Unavailable(normalized, ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "KVK profile failed for {KvkNumber}", normalized);
                return KvkCompanyProfile.Unavailable(normalized);
            }
        }

        var inUse = await LoadInUseIdsAsync(normalized, cancellationToken);
        var establishments = cached.Establishments
            .Select(e => e with { IsInUse = inUse.Contains(e.KvkEstablishmentId) })
            .OrderBy(e => e.EstablishmentNumber, StringComparer.Ordinal)
            .ToList();

        return new KvkCompanyProfile(
            KvkLookupStatus.Ok,
            cached.KvkNumber,
            cached.Name,
            cached.Address,
            cached.LegalForm,
            cached.SbiCodes,
            cached.Websites,
            establishments);
    }

    /// <summary>Live ping for Admin → Integraties. Does not fall back to the stub.</summary>
    public async Task<(bool Ok, string Message)> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Kvk, cancellationToken);
        if (string.IsNullOrWhiteSpace(secrets?.ApiKey))
        {
            return (false, "Geen KVK API-key geconfigureerd. Zonder key blijft de demo-stub actief.");
        }

        var baseUrl = KvkApiBaseUrl.Resolve(secrets.BaseUrl);
        try
        {
            var (status, body) = await SendAsync(
                secrets.ApiKey.Trim(),
                baseUrl,
                "v2/zoeken?kvkNummer=68750110&resultatenPerPagina=1",
                cancellationToken);

            if (status is HttpStatusCode.OK or HttpStatusCode.NotFound)
            {
                return (true,
                    $"Verbinding met KVK Handelsregister OK ({KvkApiBaseUrl.EnvironmentLabel(baseUrl)}). Live lookup is actief.");
            }

            if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return (false,
                    "KVK weigerde de API-key (401/403). Controleer de key én of de Base URL bij de key past: "
                    + "productie https://api.kvk.nl/api/ of test https://api.kvk.nl/test/api/.");
            }

            var detail = Truncate(body, 160);
            return (false, $"KVK gaf {(int)status}. {detail}".Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "KVK connection test failed");
            return (false, $"Geen verbinding met KVK: {Truncate(ex.Message, 180)}");
        }
    }

    private static string BuildZoekenPath(string text, string? place, int page, bool byNumber)
    {
        var qs = new List<string>
        {
            byNumber
                ? $"kvkNummer={Uri.EscapeDataString(text)}"
                : $"naam={Uri.EscapeDataString(text)}",
            "resultatenPerPagina=10",
            $"pagina={page}"
        };
        if (!byNumber && !string.IsNullOrWhiteSpace(place))
        {
            qs.Add($"plaats={Uri.EscapeDataString(place)}");
        }

        return "v2/zoeken?" + string.Join("&", qs);
    }

    private static CachedSearch ParseZoeken(string body)
    {
        var parsed = JsonSerializer.Deserialize<KvkZoekenDto>(body, JsonOptions);
        var resultaten = parsed?.Resultaten ?? [];
        var total = parsed?.Totaal ?? resultaten.Count;

        var groups = resultaten
            .Select(r => new
            {
                Kvk = DigitsOnly(r.KvkNummer) ?? "",
                Name = FirstNonEmpty(r.Handelsnaam, r.Naam, r.StatutaireNaam) ?? "Onbekende onderneming",
                Place = ExtractPlace(r),
                Type = MapHitType(r.Type),
                HasVestiging = !string.IsNullOrWhiteSpace(DigitsOnly(r.Vestigingsnummer))
            })
            .Where(r => r.Kvk.Length == 8)
            .GroupBy(r => r.Kvk, StringComparer.Ordinal);

        var hits = new List<KvkSearchHit>();
        foreach (var group in groups)
        {
            var first = group.First();
            var vestigingCount = group.Count(g => g.HasVestiging);
            if (vestigingCount == 0)
            {
                vestigingCount = group.Count();
            }

            var preferredType = group
                .Select(g => g.Type)
                .OrderBy(t => t switch
                {
                    "Rechtspersoon" => 0,
                    "Hoofdvestiging" => 1,
                    _ => 2
                })
                .First();

            hits.Add(new KvkSearchHit(
                group.Key,
                first.Name,
                first.Place,
                preferredType,
                vestigingCount,
                false));
        }

        return new CachedSearch(hits, total);
    }

    private static CachedProfile MapProfile(
        string kvkNumber,
        KvkBasisprofielDto profile,
        IReadOnlyList<KvkVestigingDto> vestigingen)
    {
        var sbi = MapSbiCodes(profile.SbiActiviteiten);
        var legalName = CompanyName(profile);
        var hqGeo = ExtractHqGeo(profile);
        var hqAddress = FormatEmbeddedHqAddress(profile);
        var legalForm = FirstNonEmpty(profile.UitgebreideRechtsvorm, profile.Rechtsvorm);
        var websites = KvkWebsiteDomain.NormalizeMany(ExtractWebsites(profile));
        var hqVisiting = ExtractAddressLine(profile.Embedded?.Hoofdvestiging?.Adressen, preferPostal: false);
        var hqPostal = ExtractAddressLine(profile.Embedded?.Hoofdvestiging?.Adressen, preferPostal: true);

        var establishments = new List<KvkEstablishmentProfile>();
        foreach (var vestiging in vestigingen)
        {
            var number = DigitsOnly(vestiging.Vestigingsnummer);
            if (string.IsNullOrWhiteSpace(number))
            {
                continue;
            }

            var isHq = IsJa(vestiging.IndHoofdvestiging);
            var visiting = ExtractAddressLine(vestiging.Adressen, preferPostal: false)
                           ?? (isHq ? hqVisiting : null);
            var postal = ExtractAddressLine(vestiging.Adressen, preferPostal: true)
                         ?? (isHq ? hqPostal : null);
            var address = FirstNonEmpty(
                              vestiging.VolledigAdres,
                              visiting?.FormattedLine,
                              isHq ? hqAddress : null)
                          ?? "";

            establishments.Add(new KvkEstablishmentProfile(
                kvkNumber,
                number,
                CompanyPublicPaths.BuildEstablishmentId(kvkNumber, number),
                FirstNonEmpty(vestiging.EersteHandelsnaam, legalName) ?? legalName,
                address,
                isHq ? hqGeo.Lat : 0,
                isHq ? hqGeo.Lng : 0,
                false,
                sbi,
                visiting,
                postal));
        }

        if (establishments.Count == 0)
        {
            var hq = profile.Embedded?.Hoofdvestiging;
            var vestigingsnummer = DigitsOnly(hq?.Vestigingsnummer);
            if (!string.IsNullOrWhiteSpace(vestigingsnummer))
            {
                establishments.Add(new KvkEstablishmentProfile(
                    kvkNumber,
                    vestigingsnummer,
                    CompanyPublicPaths.BuildEstablishmentId(kvkNumber, vestigingsnummer),
                    FirstNonEmpty(hq?.EersteHandelsnaam, legalName) ?? legalName,
                    hqAddress,
                    hqGeo.Lat,
                    hqGeo.Lng,
                    false,
                    sbi,
                    hqVisiting,
                    hqPostal));
            }
        }

        return new CachedProfile(
            kvkNumber,
            legalName,
            hqAddress,
            legalForm,
            sbi,
            websites,
            establishments);
    }

    private static IEnumerable<string?> ExtractWebsites(KvkBasisprofielDto profile)
    {
        var element = profile.Embedded?.Hoofdvestiging?.Websites;
        if (element is null || element.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            yield break;
        }

        var value = element.Value;
        if (value.ValueKind == JsonValueKind.String)
        {
            yield return value.GetString();
            yield break;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                yield return item.GetString();
                continue;
            }

            if (item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("waarde", out var waarde)
                && waarde.ValueKind == JsonValueKind.String)
            {
                yield return waarde.GetString();
            }
        }
    }

    private static KvkAddressLine? ExtractAddressLine(
        IReadOnlyList<KvkAddressDto>? addresses,
        bool preferPostal)
    {
        if (addresses is null || addresses.Count == 0)
        {
            return null;
        }

        var ordered = preferPostal
            ? addresses.OrderBy(a => a.Type?.Contains("post", StringComparison.OrdinalIgnoreCase) == true ? 0 : 1)
            : PreferBezoek(addresses);

        foreach (var address in ordered)
        {
            var type = address.Type ?? "";
            if (preferPostal && type.Contains("bezoek", StringComparison.OrdinalIgnoreCase)
                && !type.Contains("post", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!preferPostal && type.Contains("post", StringComparison.OrdinalIgnoreCase)
                && !type.Contains("bezoek", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var line = ToAddressLine(address);
            if (line is not null)
            {
                return line;
            }
        }

        return null;
    }

    private static KvkAddressLine? ToAddressLine(KvkAddressDto? address)
    {
        if (address is null)
        {
            return null;
        }

        var nested = address.BinnenlandsAdres;
        var street = FirstNonEmpty(address.Straatnaam, nested?.Straatnaam) ?? "";
        var house = FirstNonEmpty(
            FormatJsonNumber(address.Huisnummer),
            nested is null ? null : FormatJsonNumber(nested.Huisnummer)) ?? "";
        var letter = FirstNonEmpty(address.Huisletter, nested?.Huisletter);
        var postcode = FirstNonEmpty(address.Postcode, nested?.Postcode) ?? "";
        var place = FirstNonEmpty(address.Plaats, nested?.Plaats) ?? "";

        if (string.IsNullOrWhiteSpace(street)
            && string.IsNullOrWhiteSpace(postcode)
            && string.IsNullOrWhiteSpace(place)
            && !string.IsNullOrWhiteSpace(address.VolledigAdres))
        {
            // Fall back: put volledigAdres in Street so FormattedLine still shows something.
            return new KvkAddressLine(address.VolledigAdres.Trim(), "", null, "", "");
        }

        if (string.IsNullOrWhiteSpace(street)
            && string.IsNullOrWhiteSpace(postcode)
            && string.IsNullOrWhiteSpace(place))
        {
            return null;
        }

        return new KvkAddressLine(street, house, letter, postcode, place);
    }

    private static string ExtractPlace(KvkZoekenResultaatDto r)
    {
        return FirstNonEmpty(
                   r.Adres?.Plaats,
                   r.Adres?.BinnenlandsAdres?.Plaats,
                   r.Plaats)
               ?? "";
    }

    private static string MapHitType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Rechtspersoon";
        }

        if (raw.Contains("neven", StringComparison.OrdinalIgnoreCase))
        {
            return "Nevenvestiging";
        }

        if (raw.Contains("hoofd", StringComparison.OrdinalIgnoreCase))
        {
            return "Hoofdvestiging";
        }

        return "Rechtspersoon";
    }

    private async Task<bool> HasApiKeyAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Kvk, cancellationToken);
        return !string.IsNullOrWhiteSpace(secrets?.ApiKey);
    }

    private async Task<KvkBasisprofielDto?> GetBasisprofielAsync(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var (status, body) = await SendTrackedAsync(
            $"v1/basisprofielen/{Uri.EscapeDataString(kvkNumber)}?geoData=true",
            KvkUsageCallTypes.Basisprofiel,
            cancellationToken);

        if (status == HttpStatusCode.NotFound)
        {
            return null;
        }

        EnsureSuccess(status, body);
        return JsonSerializer.Deserialize<KvkBasisprofielDto>(body, JsonOptions);
    }

    private async Task<IReadOnlyList<KvkVestigingDto>> GetVestigingenAsync(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var (status, body) = await SendTrackedAsync(
            $"v1/basisprofielen/{Uri.EscapeDataString(kvkNumber)}/vestigingen",
            KvkUsageCallTypes.Vestigingen,
            cancellationToken);

        if (status == HttpStatusCode.NotFound)
        {
            return [];
        }

        EnsureSuccess(status, body);
        var parsed = JsonSerializer.Deserialize<KvkVestigingenDto>(body, JsonOptions);
        return parsed?.Vestigingen
               ?? parsed?.Embedded?.Vestigingen
               ?? [];
    }

    private async Task<(HttpStatusCode Status, string Body)> SendTrackedAsync(
        string relativePath,
        string callType,
        CancellationToken cancellationToken)
    {
        var (apiKey, baseUrl) = await RequireLiveAsync(cancellationToken);
        var result = await SendAsync(apiKey, baseUrl, relativePath, cancellationToken);
        if (_usage is not null && (int)result.Status < 500)
        {
            try
            {
                await _usage.IncrementAsync(callType, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "KVK usage counter failed for {CallType}", callType);
            }
        }

        return result;
    }

    private async Task<(string ApiKey, string BaseUrl)> RequireLiveAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Kvk, cancellationToken)
            ?? throw new KvkServiceUnavailableException("Geen KVK API-key geconfigureerd.");
        var apiKey = secrets.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new KvkServiceUnavailableException("Geen KVK API-key geconfigureerd.");
        }

        return (apiKey, KvkApiBaseUrl.Resolve(secrets.BaseUrl));
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(
        string apiKey,
        string baseUrl,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var uri = new Uri(new Uri(baseUrl, UriKind.Absolute), relativePath);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("apikey", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/hal+json"));

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.StatusCode, body);
    }

    private static void EnsureSuccess(HttpStatusCode status, string body)
    {
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new KvkAuthException(status);
        }

        if ((int)status >= 500 || status == HttpStatusCode.RequestTimeout || (int)status == 429)
        {
            throw new KvkServiceUnavailableException(
                "KVK-dienst is tijdelijk niet beschikbaar. Je kunt doorgaan; verificatie volgt later.");
        }

        if ((int)status >= 400)
        {
            throw new KvkServiceUnavailableException(
                $"KVK gaf {(int)status}: {Truncate(body, 160)}");
        }
    }

    private async Task<HashSet<string>> LoadInUseIdsAsync(string kvkNumber, CancellationToken cancellationToken)
    {
        var ids = await _db.Companies
            .AsNoTracking()
            .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
            .Select(c => c.KvkEstablishmentId!)
            .ToListAsync(cancellationToken);
        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> LoadOnLobsyAsync(
        IEnumerable<string> kvkNumbers,
        CancellationToken cancellationToken)
    {
        var list = kvkNumbers.Distinct(StringComparer.Ordinal).ToList();
        if (list.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var matches = await (
            from c in _db.Companies.AsNoTracking()
            join uc in _db.UserCompanies.AsNoTracking() on c.Id equals uc.CompanyId
            join u in _db.Users.AsNoTracking() on uc.UserId equals u.Id
            where list.Contains(c.KvkNumber) && u.IsActive
            select c.KvkNumber).Distinct().ToListAsync(cancellationToken);

        return new HashSet<string>(matches, StringComparer.Ordinal);
    }

    private static KvkCompanyResult MapCompany(string kvkNumber, KvkBasisprofielDto profile)
    {
        var name = CompanyName(profile);
        var address = FormatEmbeddedHqAddress(profile);
        return new KvkCompanyResult(kvkNumber, name, address, MapSbiCodes(profile.SbiActiviteiten));
    }

    private static List<KvkEstablishmentResult> MapVestigingen(
        string kvkNumber,
        IReadOnlyList<KvkVestigingDto> vestigingen,
        string legalName,
        IReadOnlyList<string> sbi,
        (double Lat, double Lng) hqGeo,
        string hqAddress)
    {
        var items = new List<KvkEstablishmentResult>();
        foreach (var vestiging in vestigingen)
        {
            var number = DigitsOnly(vestiging.Vestigingsnummer);
            if (string.IsNullOrWhiteSpace(number))
            {
                continue;
            }

            var isHq = IsJa(vestiging.IndHoofdvestiging);
            var address = FirstNonEmpty(vestiging.VolledigAdres, isHq ? hqAddress : null) ?? "";
            items.Add(new KvkEstablishmentResult(
                kvkNumber,
                number,
                CompanyPublicPaths.BuildEstablishmentId(kvkNumber, number),
                FirstNonEmpty(vestiging.EersteHandelsnaam, legalName) ?? legalName,
                address,
                isHq ? hqGeo.Lat : 0,
                isHq ? hqGeo.Lng : 0,
                false,
                sbi));
        }

        return items;
    }

    private static string CompanyName(KvkBasisprofielDto profile)
        => FirstNonEmpty(
               profile.StatutaireNaam,
               profile.Naam,
               profile.Handelsnamen?.Select(h => h.Naam).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
               profile.Embedded?.Hoofdvestiging?.EersteHandelsnaam)
           ?? "Onbekende onderneming";

    private static IReadOnlyList<string> MapSbiCodes(IReadOnlyList<KvkSbiDto>? activities)
    {
        if (activities is null || activities.Count == 0)
        {
            return [];
        }

        return activities
            .OrderByDescending(a => IsJa(a.IndHoofdactiviteit))
            .Select(a => ReadCode(a.SbiCode))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static (double Lat, double Lng) ExtractHqGeo(KvkBasisprofielDto profile)
    {
        var addresses = profile.Embedded?.Hoofdvestiging?.Adressen;
        if (addresses is null)
        {
            return (0, 0);
        }

        foreach (var address in PreferBezoek(addresses))
        {
            var geo = address.GeoData;
            if (geo?.GpsLatitude is double lat && geo.GpsLongitude is double lng
                && Math.Abs(lat) > 0.01 && Math.Abs(lng) > 0.01)
            {
                return (lat, lng);
            }
        }

        return (0, 0);
    }

    private static string FormatEmbeddedHqAddress(KvkBasisprofielDto profile)
    {
        var addresses = profile.Embedded?.Hoofdvestiging?.Adressen;
        if (addresses is null || addresses.Count == 0)
        {
            return "";
        }

        foreach (var address in PreferBezoek(addresses))
        {
            var formatted = FormatAddress(address);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }
        }

        return "";
    }

    private static IEnumerable<KvkAddressDto> PreferBezoek(IEnumerable<KvkAddressDto> addresses)
        => addresses.OrderBy(a =>
            a.Type?.Contains("bezoek", StringComparison.OrdinalIgnoreCase) == true ? 0 : 1);

    private static string FormatAddress(KvkAddressDto? address)
    {
        if (address is null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(address.VolledigAdres))
        {
            return address.VolledigAdres.Trim();
        }

        var nested = FormatAddress(address.BinnenlandsAdres);
        if (!string.IsNullOrWhiteSpace(nested))
        {
            return nested;
        }

        var line = ToAddressLine(address);
        return line?.FormattedLine ?? "";
    }

    private static string? FormatJsonNumber(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.String => element.GetString(),
            _ => null
        };

    private static string? ReadCode(JsonElement element)
    {
        var raw = FormatJsonNumber(element)?.Trim();
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static string? DigitsOnly(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length is >= 1 and <= 12 ? digits : null;
    }

    private static bool IsJa(string? value)
        => string.Equals(value, "Ja", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "…";
    }

    private static string NormalizeCachePart(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLowerInvariant();

    private sealed class KvkAuthException(HttpStatusCode statusCode) : Exception("KVK API-key rejected")
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
    }

    private sealed record CachedSearch(IReadOnlyList<KvkSearchHit> Hits, int Total);

    private sealed record CachedProfile(
        string KvkNumber,
        string Name,
        string Address,
        string? LegalForm,
        IReadOnlyList<string> SbiCodes,
        IReadOnlyList<string> Websites,
        IReadOnlyList<KvkEstablishmentProfile> Establishments);

    private sealed class KvkZoekenDto
    {
        public int? Totaal { get; set; }
        public List<KvkZoekenResultaatDto>? Resultaten { get; set; }
    }

    private sealed class KvkZoekenResultaatDto
    {
        public string? KvkNummer { get; set; }
        public string? Vestigingsnummer { get; set; }
        public string? Naam { get; set; }
        public string? Handelsnaam { get; set; }
        public string? StatutaireNaam { get; set; }
        public string? Type { get; set; }
        public string? Plaats { get; set; }
        public KvkAddressDto? Adres { get; set; }
    }

    private sealed class KvkBasisprofielDto
    {
        public string? KvkNummer { get; set; }
        public string? Naam { get; set; }
        public string? StatutaireNaam { get; set; }
        public string? Rechtsvorm { get; set; }
        public string? UitgebreideRechtsvorm { get; set; }
        public List<KvkHandelsnaamDto>? Handelsnamen { get; set; }
        public List<KvkSbiDto>? SbiActiviteiten { get; set; }

        [JsonPropertyName("_embedded")]
        public KvkBasisEmbeddedDto? Embedded { get; set; }
    }

    private sealed class KvkBasisEmbeddedDto
    {
        public KvkHoofdvestigingDto? Hoofdvestiging { get; set; }
    }

    private sealed class KvkHoofdvestigingDto
    {
        public string? Vestigingsnummer { get; set; }
        public string? EersteHandelsnaam { get; set; }
        public List<KvkAddressDto>? Adressen { get; set; }

        /// <summary>String array or objects with <c>waarde</c> — parsed in <see cref="ExtractWebsites"/>.</summary>
        public JsonElement? Websites { get; set; }
    }

    private sealed class KvkVestigingenDto
    {
        public List<KvkVestigingDto>? Vestigingen { get; set; }

        [JsonPropertyName("_embedded")]
        public KvkVestigingenEmbeddedDto? Embedded { get; set; }
    }

    private sealed class KvkVestigingenEmbeddedDto
    {
        public List<KvkVestigingDto>? Vestigingen { get; set; }
    }

    private sealed class KvkVestigingDto
    {
        public string? Vestigingsnummer { get; set; }
        public string? EersteHandelsnaam { get; set; }
        public string? IndHoofdvestiging { get; set; }
        public string? VolledigAdres { get; set; }
        public List<KvkAddressDto>? Adressen { get; set; }
    }

    private sealed class KvkHandelsnaamDto
    {
        public string? Naam { get; set; }
    }

    private sealed class KvkSbiDto
    {
        public JsonElement SbiCode { get; set; }
        public string? IndHoofdactiviteit { get; set; }
    }

    private sealed class KvkAddressDto
    {
        public string? Type { get; set; }
        public string? VolledigAdres { get; set; }
        public string? Straatnaam { get; set; }
        public JsonElement Huisnummer { get; set; }
        public string? Huisletter { get; set; }
        public string? Postcode { get; set; }
        public string? Plaats { get; set; }
        public KvkAddressDto? BinnenlandsAdres { get; set; }
        public KvkGeoDataDto? GeoData { get; set; }
    }

    private sealed class KvkGeoDataDto
    {
        public double? GpsLatitude { get; set; }
        public double? GpsLongitude { get; set; }
    }
}
