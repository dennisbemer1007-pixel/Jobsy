using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;

namespace Jobsy.Core.Interfaces;

public interface IKvkService
{
    Task<KvkCompanyResult?> GetByKvkNumberAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns establishments for a KVK number.
    /// Throws <see cref="KvkServiceUnavailableException"/> on transient API failure.
    /// Empty list means the number is unknown / has no vestigingen (not an outage).
    /// </summary>
    Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lookup that distinguishes API outage from "not found" without throwing.
    /// </summary>
    Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Name or number search via KVK Zoeken. Text of exactly 8 digits searches by
    /// <c>kvkNummer</c>; otherwise by <c>naam</c> (min 3 characters after trim).
    /// </summary>
    /// <exception cref="ArgumentException">When name search text is shorter than 3 characters.</exception>
    Task<KvkSearchResult> SearchAsync(
        KvkSearchQuery query,
        CancellationToken cancellationToken = default)
        => Task.FromResult(KvkSearchResult.Unavailable("KVK-zoeken is niet beschikbaar in deze stub."));

    /// <summary>
    /// Full company profile (basisprofiel + vestigingen) fetched only when a search hit is selected.
    /// </summary>
    Task<KvkCompanyProfile> GetProfileAsync(
        string kvkNumber,
        CancellationToken cancellationToken = default)
        => Task.FromResult(KvkCompanyProfile.Unavailable(kvkNumber ?? "", "KVK-profiel is niet beschikbaar in deze stub."));
}

public enum KvkLookupStatus
{
    Ok = 0,
    NotFound = 1,
    Unavailable = 2
}

public sealed record KvkEstablishmentsLookup(
    KvkLookupStatus Status,
    IReadOnlyList<KvkEstablishmentResult> Establishments,
    string? Message = null)
{
    public static KvkEstablishmentsLookup Ok(IReadOnlyList<KvkEstablishmentResult> items)
        => new(KvkLookupStatus.Ok, items);

    public static KvkEstablishmentsLookup NotFound()
        => new(KvkLookupStatus.NotFound, Array.Empty<KvkEstablishmentResult>(),
            "Geen vestigingen gevonden voor dit KVK-nummer.");

    public static KvkEstablishmentsLookup Unavailable(string? message = null)
        => new(KvkLookupStatus.Unavailable, Array.Empty<KvkEstablishmentResult>(),
            message ?? "KVK-dienst is tijdelijk niet beschikbaar. Je kunt doorgaan; verificatie volgt later.");
}

public record KvkCompanyResult(
    string KvkNumber,
    string Name,
    string Address,
    IReadOnlyList<string>? SbiCodes = null,
    CompanyLegalForm? LegalForm = null)
{
    public IReadOnlyList<string> EffectiveSbiCodes => SbiCodes ?? Array.Empty<string>();
}

public record KvkEstablishmentResult(
    string KvkNumber,
    string EstablishmentNumber,
    string KvkEstablishmentId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    bool IsInUse,
    IReadOnlyList<string>? SbiCodes = null)
{
    public IReadOnlyList<string> EffectiveSbiCodes => SbiCodes ?? Array.Empty<string>();
}

public sealed record KvkSearchQuery(string Text, string? Place = null, int Page = 1);

public sealed record KvkSearchHit(
    string KvkNumber,
    string Name,
    string Place,
    string Type,
    int? VestigingCount,
    bool IsOnLobsy);

public sealed record KvkSearchResult(
    KvkLookupStatus Status,
    IReadOnlyList<KvkSearchHit> Hits,
    int Total,
    string? Message = null)
{
    public static KvkSearchResult Ok(IReadOnlyList<KvkSearchHit> hits, int total)
        => new(KvkLookupStatus.Ok, hits, total);

    public static KvkSearchResult Unavailable(string? message = null)
        => new(
            KvkLookupStatus.Unavailable,
            Array.Empty<KvkSearchHit>(),
            0,
            message ?? "KVK-dienst is tijdelijk niet beschikbaar. Je kunt doorgaan; verificatie volgt later.");
}

/// <summary>Street + house + postcode + place (public KVK data; used for letter preview).</summary>
public sealed record KvkAddressLine(
    string Street,
    string HouseNumber,
    string? HouseLetter,
    string Postcode,
    string Place)
{
    public string FormattedLine
    {
        get
        {
            var house = string.Concat(HouseNumber, HouseLetter).Trim();
            var street = string.IsNullOrWhiteSpace(house)
                ? Street.Trim()
                : $"{Street.Trim()} {house}".Trim();
            var city = string.Join(" ", new[] { Postcode, Place }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join(", ", new[] { street, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}

public sealed record KvkEstablishmentProfile(
    string KvkNumber,
    string EstablishmentNumber,
    string KvkEstablishmentId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    bool IsInUse,
    IReadOnlyList<string> SbiCodes,
    KvkAddressLine? VisitingAddress,
    KvkAddressLine? PostalAddress);

public sealed record KvkCompanyProfile(
    KvkLookupStatus Status,
    string KvkNumber,
    string Name,
    string Address,
    string? LegalForm,
    IReadOnlyList<string> SbiCodes,
    IReadOnlyList<string> Websites,
    IReadOnlyList<KvkEstablishmentProfile> Establishments,
    string? Message = null)
{
    public static KvkCompanyProfile NotFound(string kvkNumber)
        => new(
            KvkLookupStatus.NotFound,
            kvkNumber,
            "",
            "",
            null,
            [],
            [],
            [],
            "Geen bedrijf gevonden voor dit KVK-nummer.");

    public static KvkCompanyProfile Unavailable(string kvkNumber, string? message = null)
        => new(
            KvkLookupStatus.Unavailable,
            kvkNumber,
            "",
            "",
            null,
            [],
            [],
            [],
            message ?? "KVK-dienst is tijdelijk niet beschikbaar. Je kunt doorgaan; verificatie volgt later.");
}
