using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Live KVK Handelsregister (Zoeken / Basisprofiel / vestigingen). Falls back to
/// <see cref="KvkServiceStub"/> when no API-key is configured so demos keep working.
/// </summary>
public sealed class KvkHandelsregisterService : IKvkService
{
    public const string HttpClientName = "Kvk";

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

    public KvkHandelsregisterService(
        JobsyDbContext db,
        IIntegrationCredentialService credentials,
        IHttpClientFactory httpClientFactory,
        KvkServiceStub stub,
        ILogger<KvkHandelsregisterService> logger)
    {
        _db = db;
        _credentials = credentials;
        _httpClientFactory = httpClientFactory;
        _stub = stub;
        _logger = logger;
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

            var inUse = await _db.Companies
                .AsNoTracking()
                .Where(c => c.KvkNumber == normalized && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

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

    private async Task<bool> HasApiKeyAsync(CancellationToken cancellationToken)
    {
        var secrets = await _credentials.GetSecretsAsync(IntegrationKey.Kvk, cancellationToken);
        return !string.IsNullOrWhiteSpace(secrets?.ApiKey);
    }

    private async Task<KvkBasisprofielDto?> GetBasisprofielAsync(
        string kvkNumber,
        CancellationToken cancellationToken)
    {
        var (apiKey, baseUrl) = await RequireLiveAsync(cancellationToken);
        var (status, body) = await SendAsync(
            apiKey,
            baseUrl,
            $"v1/basisprofielen/{Uri.EscapeDataString(kvkNumber)}?geoData=true",
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
        var (apiKey, baseUrl) = await RequireLiveAsync(cancellationToken);
        var (status, body) = await SendAsync(
            apiKey,
            baseUrl,
            $"v1/basisprofielen/{Uri.EscapeDataString(kvkNumber)}/vestigingen",
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

        var house = FormatJsonNumber(address.Huisnummer);
        var street = string.Join("", new[]
        {
            address.Straatnaam,
            string.IsNullOrWhiteSpace(house) ? null : " " + house,
            address.Huisletter
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var city = string.Join(" ", new[] { address.Postcode, address.Plaats }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.Join(", ", new[] { street, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
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

    private sealed class KvkAuthException(HttpStatusCode statusCode) : Exception("KVK API-key rejected")
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
    }

    private sealed class KvkBasisprofielDto
    {
        public string? KvkNummer { get; set; }
        public string? Naam { get; set; }
        public string? StatutaireNaam { get; set; }
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
